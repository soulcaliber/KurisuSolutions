﻿#region Copyright © 2015 Kurisu Solutions
// All rights are reserved. Transmission or reproduction in part or whole,
// any form or by any means, mechanical, electronical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
// 
// Document:	Items/CoreItem.cs
// Date:		22/09/2015
// Author:		Robin Kurisu
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Activator.Base;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Activator.Items
{
    public class CoreItem
    {
        internal virtual int Id { get; set; }
        internal virtual int Priority { get; set; }
        internal virtual int Duration { get; set; }
        internal virtual bool Craving { get; set; }
        internal virtual string Name { get; set; }
        internal virtual string DisplayName { get; set; }
        internal virtual float Range { get; set; }
        internal virtual MenuType[] Category { get; set; }
        internal virtual MapType[] Maps { get; set; }

        internal virtual int DefaultMP { get; set; }
        internal virtual int DefaultHP { get; set; }

        public Menu Menu { get; private set; }
        public Menu Parent { get { return Menu.Parent; } }
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        public Champion Tar
        {
            get
            {
                return
                    Activator.Heroes.Where(
                        hero => hero.Player.IsEnemy && hero.Player.IsValidTarget(Range) &&
                               !hero.Player.IsZombie).OrderBy(x => x.Player.Distance(Game.CursorPos)).FirstOrDefault();
            }
        }

        public static IEnumerable<CoreItem> PriorityList()
        {
            var hpi = from ii in Lists.Items
                      where  LeagueSharp.Common.Items.CanUseItem(ii.Id) && ii.Craving
                      orderby ii.Menu.Item("prior" + ii.Name).GetValue<Slider>().Value descending 
                      select ii;

            return hpi;
        }

        public bool IsReady()
        {
            return LeagueSharp.Common.Items.CanUseItem(Id);
        }
           
        public void UseItem(bool combo = false)
        {
            Craving = true;

            if (!combo || Activator.Origin.Item("usecombo").GetValue<KeyBind>().Active)
            {
                if (PriorityList().Any())
                {
                    if (Name == PriorityList().First().Name)
                    {
                        if (Utils.GameTimeTickCount - Activator.LastUsedTimeStamp > Duration)
                        {
                            LeagueSharp.Common.Items.UseItem(Id);
                            Activator.LastUsedTimeStamp = Utils.GameTimeTickCount;
                            Activator.LastUsedDuration = Duration;
                        }
                    }
                }
            }

            Craving = false;
        }

        public void UseItem(Obj_AI_Base target, bool combo = false)
        {
            Craving = true;

            if (!combo || Activator.Origin.Item("usecombo").GetValue<KeyBind>().Active)
            {
                if (PriorityList().Any())
                {
                    if (Name == PriorityList().First().Name)
                    {
                        if (Utils.GameTimeTickCount - Activator.LastUsedTimeStamp > Duration)
                        {
                            LeagueSharp.Common.Items.UseItem(Id, target);
                            Activator.LastUsedTimeStamp = Utils.GameTimeTickCount;
                            Activator.LastUsedDuration = Duration;
                        }
                    }
                }
            }

            Craving = false;
        }

        public void UseItem(Vector3 pos, bool combo = false)
        {
            Craving = true;

            if (!combo || Activator.Origin.Item("usecombo").GetValue<KeyBind>().Active)
            {
                if (PriorityList().Any() && Name == PriorityList().First().Name)
                {
                    if (Utils.GameTimeTickCount - Activator.LastUsedTimeStamp > Duration)
                    {
                        LeagueSharp.Common.Items.UseItem(Id, pos);
                        Activator.LastUsedTimeStamp = Utils.GameTimeTickCount;
                        Activator.LastUsedDuration = Duration;
                    }
                }
            }

            Craving = false;
        }

        public CoreItem CreateMenu(Menu root)
        {
            var usefname = DisplayName ?? Name;

            Menu = new Menu(Name, "m" + Name);
            Menu.AddItem(new MenuItem("use" + Name, "Use " + usefname)).SetValue(true);
            Menu.AddItem(new MenuItem("prior" + Name, DisplayName + " Priority")).SetValue(new Slider(Priority, 1, 7));

 
            if (Category.Any(t => t == MenuType.SelfLowHP) &&
               (Name.Contains("Potion") || Name.Contains("Flask") || Name.Contains("Biscuit")))
            {
                Menu.AddItem(new MenuItem("use" + Name + "cbat", "Predict Damage")).SetValue(true);
            }

            if (Category.Any(t => t == MenuType.EnemyLowHP))
            {
                Menu.AddItem(new MenuItem("enemylowhp" + Name + "pct", "Use on Enemy HP % <="))
                    .SetValue(new Slider(DefaultHP));
            }

            if (Category.Any(t => t == MenuType.SelfLowHP))
                Menu.AddItem(new MenuItem("selflowhp" + Name + "pct", "Use on Hero HP % <="))
                    .SetValue(new Slider(DefaultHP <= 50 || DefaultHP > 90 ? DefaultHP : 45));

            if (Category.Any(t => t == MenuType.SelfMuchHP))
                Menu.AddItem(new MenuItem("selfmuchhp" + Name + "pct", "Use on Hero Dmg Dealt % >="))
                    .SetValue(new Slider(DefaultHP > 45 ? 55 : DefaultHP < 35 ? 45 : 40));

            if (Category.Any(t => t == MenuType.SelfLowMP))
                Menu.AddItem(new MenuItem("selflowmp" + Name + "pct", "Use on Hero Mana % <="))
                    .SetValue(new Slider(DefaultMP));

            if (Category.Any(t => t == MenuType.SelfCount))
                Menu.AddItem(new MenuItem("selfcount" + Name, "Use On Enemy Near Count >="))
                    .SetValue(new Slider(2, 1, 5));

            if (Category.Any(t => t == MenuType.SelfMinMP))
                Menu.AddItem(new MenuItem("selfminmp" + Name + "pct", "Minimum Mana %")).SetValue(new Slider(55));

            if (Category.Any(t => t == MenuType.SelfMinHP))
                Menu.AddItem(new MenuItem("selfminhp" + Name + "pct", "Minimum HP %")).SetValue(new Slider(55));

            if (Category.Any(t => t == MenuType.Zhonyas))
            {
                Menu.AddItem(new MenuItem("use" + Name + "norm", "Use on Dangerous (Spells)")).SetValue(false);
                Menu.AddItem(new MenuItem("use" + Name + "ulti", "Use on Dangerous (Ultimates Only)")).SetValue(true);
            }

            if (Category.Any(t => t == MenuType.Cleanse))
            {
                var ccmenu = new Menu(Name + " Buff Types", Name.ToLower() + "cdeb");
                var ssmenu = new Menu(Name + " Misc Buffs", Name.ToLower() + "xspe");

                foreach (var b in Data.BuffData.BuffList.Where(x => x.MenuName != null && (x.Cleanse || x.DoT)))
                {
                    string xdot = b.DoT && b.Cleanse ? "[Danger]" : (b.DoT ? "[DoT]" : "[Danger]");

                    if (b.Champion != null)
                        foreach (var ene in Activator.Heroes.Where(x => x.Player.IsEnemy && b.Champion == x.Player.ChampionName))
                            ssmenu.AddItem(new MenuItem(Name + b.Name + "cc", b.MenuName + " " + xdot)).SetValue(true);
                    else
                        ssmenu.AddItem(new MenuItem(Name + b.Name + "cc", b.MenuName + " " + xdot)).SetValue(true);
                }

                ccmenu.AddItem(new MenuItem(Name + "cignote", "Ignite")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "cexhaust", "Exhaust")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "cstun", "Stuns")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "ccharm", "Charms")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "ctaunt", "Taunts")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "cfear", "Fears")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "cflee", "Flee")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "csnare", "Snares")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "csilence", "Silences")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "csupp", "Supression")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "cpolymorph", "Polymorphs")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "cblind", "Blinds")).SetValue(true);
                ccmenu.AddItem(new MenuItem(Name + "cslow", "Slows")).SetValue(false);
                ccmenu.AddItem(new MenuItem(Name + "cpoison", "Poisons")).SetValue(true);
                Menu.AddSubMenu(ccmenu);
                Menu.AddSubMenu(ssmenu);

                Menu.AddItem(new MenuItem("use" + Name + "number", "Min Buffs to Use")).SetValue(new Slider(DefaultHP/5, 1, 5));
                Menu.AddItem(new MenuItem("use" + Name + "time", "Min Durration to Use (sec)")).SetValue(new Slider(1, 1, 5));
                Menu.AddItem(new MenuItem("use" + Name + "delay", "Activation Delay")).SetValue(new Slider(150, 0, 500));
                Menu.AddItem(new MenuItem("use" + Name + "od", "Use for Dangerous Only")).SetValue(false);
                Menu.AddItem(new MenuItem("use" + Name + "dot", "Use for DoTs only if HP% <")).SetValue(new Slider(35));
            }

            if (Category.Any(t => t == MenuType.ActiveCheck))
                Menu.AddItem(new MenuItem("mode" + Name, "Mode: "))
                    .SetValue(new StringList(new[] { "Always", "Combo" }));

            root.AddSubMenu(Menu);
            return this;
        }

        public virtual void OnTick(EventArgs args)
        {

        }
    }
}
