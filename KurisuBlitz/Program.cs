﻿using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace KurisuBlitz
{
    //  _____ _ _ _                       _   
    // | __  | |_| |_ ___ ___ ___ ___ ___| |_ 
    // | __ -| | |  _|- _|  _|  _| .'|   | '_|
    // |_____|_|_|_| |___|___|_| |__,|_|_|_,_|
    //  Copyright © Kurisu Solutions 2015

    internal class Program
    {
        private static Menu _menu;
        private static Spell _q, _e, _x, _r;
        private static readonly Obj_AI_Hero Me = ObjectManager.Player;

        static void Main(string[] args)
        {
            Console.WriteLine("Blitzcrank injected...");
            CustomEvents.Game.OnGameLoad += BlitzOnLoad;
        }

        private static void BlitzOnLoad(EventArgs args)
        {
            if (Me.ChampionName != "Blitzcrank")
                return;

            // Set spells      
            _q = new Spell(SpellSlot.Q, 950f);
            _q.SetSkillshot(0.25f, 70f, 1800f, true, SkillshotType.SkillshotLine);

            _x = new Spell(SpellSlot.Q, 950f);
            _x.SetSkillshot(0.50f, 70f, 1800f, false, SkillshotType.SkillshotLine);

            _e = new Spell(SpellSlot.E, 150f);
            _r = new Spell(SpellSlot.R, 550f);

            // Load Menu
            _menu = new Menu("Kurisu's Blitz", "blitz", true);

            var blitzTs = new Menu(":: Selector", "tmenu");
            TargetSelector.AddToMenu(blitzTs);
            _menu.AddSubMenu(blitzTs);

            var blitzOrb = new Menu(":: Orbwalker", "omenu");
            var orbwalker = new Orbwalking.Orbwalker(blitzOrb);
            _menu.AddSubMenu(blitzOrb);

            var menuK = new Menu(":: Keybinds", "kmenu");
            menuK.AddItem(new MenuItem("grabkey", "Grab Key (active)")).SetValue(new KeyBind('T', KeyBindType.Press));
            menuK.AddItem(new MenuItem("combokey", "Combo (active)")).SetValue(new KeyBind(32, KeyBindType.Press));
            _menu.AddSubMenu(menuK);

            var menuD = new Menu(":: Drawings", "dmenu");
            menuD.AddItem(new MenuItem("drawQ", "Draw Q")).SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            menuD.AddItem(new MenuItem("drawR", "Draw R")).SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            menuD.AddItem(new MenuItem("drawT", "Draw Target")).SetValue(true);
            _menu.AddSubMenu(menuD);

            var menuH = new Menu(":: Hero Config", "enemies");
            foreach (var obj in ObjectManager.Get<Obj_AI_Hero>().Where(obj => obj.Team != Me.Team))
            {
                menuH.AddItem(new MenuItem("dograb" + obj.ChampionName, obj.ChampionName))
                    .SetValue(new StringList(new[] { "Dont Grab ", "Normal Grab ", "R + Grab!" },
                        TargetSelector.GetPriority(obj) >= 4 ? 2 : 1));
            }

            _menu.AddSubMenu(menuH);

            var spellmenu = new Menu(":: Main Settings", "mmenu");

            var menuQ = new Menu("Q Menu", "qmenu");
            menuQ.AddItem(new MenuItem("usecomboq", "Use in Combo")).SetValue(true);
            menuQ.AddItem(new MenuItem("qdashing", "Q on Dashing Enemies")).SetValue(true);
            menuQ.AddItem(new MenuItem("qimmobil", "Q on Immobile Enemies")).SetValue(true);
            menuQ.AddItem(new MenuItem("interruptq", "Use for Interrupt")).SetValue(true);
            menuQ.AddItem(new MenuItem("secureq", "Use for Killsteal")).SetValue(false);
            spellmenu.AddSubMenu(menuQ);

            var menuE = new Menu("E Menu", "emenu");
            menuE.AddItem(new MenuItem("usecomboe", "Use in Combo")).SetValue(true);
            menuE.AddItem(new MenuItem("interrupte", "Use for Interrupt")).SetValue(true);
            menuE.AddItem(new MenuItem("securee", "Use for Killsteal")).SetValue(false);
            spellmenu.AddSubMenu(menuE);

            var menuR = new Menu("R Menu", "rmenu");
            menuR.AddItem(new MenuItem("usecombor", "Use in Combo")).SetValue(true);
            menuR.AddItem(new MenuItem("interruptr", "Use for Interrupt")).SetValue(true);
            menuR.AddItem(new MenuItem("securer", "Use for Killsteal")).SetValue(false);
            spellmenu.AddSubMenu(menuR);

            _menu.AddSubMenu(spellmenu);

            var menuM = new Menu(":: Other/Misc", "bmisc");
            menuM.AddItem(new MenuItem("hitchanceq", "Q Hitchance 1-Low, 4-Very High")).SetValue(new Slider(4, 1, 4));
            menuM.AddItem(new MenuItem("mindist", "Mininum Distance to Q")).SetValue(new Slider(255, 0, (int)_q.Range));
            menuM.AddItem(new MenuItem("maxdist", "Maximum Distance to Q")).SetValue(new Slider((int)_q.Range, 0, (int)_q.Range));
            menuM.AddItem(new MenuItem("hnd", "Dont grab if below health %")).SetValue(new Slider(0));


            _menu.AddSubMenu(menuM);
            _menu.AddToMainMenu();

            // events
            Drawing.OnDraw += BlitzOnDraw;
            Game.OnUpdate += BlitzOnUpdate;
            Interrupter.OnPossibleToInterrupt += BlitzOnInterruptableSpell;

            Game.PrintChat("<font color=\"#FF9900\"><b>KurisuBlitz:</b></font> Loaded");

        }

        private static void BlitzOnInterruptableSpell(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (_menu.Item("interruptq").GetValue<bool>() && _q.IsReady())
            {
                if (unit.Distance(Me.ServerPosition, true) <= _q.RangeSqr)
                    _q.CastIfHitchanceEquals(unit, HitChance.Medium);
            }

            if (_menu.Item("interruptr").GetValue<bool>() && _r.IsReady())
            {
                if (unit.Distance(Me.ServerPosition, true) <= _r.RangeSqr)
                    _r.Cast();
            }

            if (_menu.Item("interrupte").GetValue<bool>() && _e.IsReady())
            {
                if (unit.Distance(Me.ServerPosition, true) <= _e.RangeSqr)
                    _e.CastOnUnit(Me);
            }
        }

        private static void BlitzOnDraw(EventArgs args)
        {
            var target = TargetSelector.GetTarget(_q.Range * 2, TargetSelector.DamageType.Physical);

            if (!Me.IsDead)
            {
                var rcircle = _menu.Item("drawR").GetValue<Circle>();
                var qcircle = _menu.Item("drawQ").GetValue<Circle>();

                if (qcircle.Active)
                    Render.Circle.DrawCircle(Me.Position, _q.Range, qcircle.Color);

                if (rcircle.Active)
                    Render.Circle.DrawCircle(Me.Position, _r.Range, qcircle.Color);

                if (target.IsValidTarget(_q.Range * 2) && _menu.Item("drawT").GetValue<bool>())
                    Render.Circle.DrawCircle(target.Position, target.BoundingRadius - 30, Color.Yellow, 3);
            }
        }

        private static void BlitzOnUpdate(EventArgs args)
        {
            Secure(_menu.Item("secureq").GetValue<bool>(), 
                   _menu.Item("securee").GetValue<bool>(),
                   _menu.Item("securer").GetValue<bool>());

            if (Me.HealthPercent >= _menu.Item("hnd").GetValue<Slider>().Value)
            {
                AutoCast(_menu.Item("qdashing").GetValue<bool>(),
                         _menu.Item("qimmobil").GetValue<bool>());

                if (_menu.Item("combokey").GetValue<KeyBind>().Active)
                {
                    Combo(_menu.Item("usecomboq").GetValue<bool>(),
                          _menu.Item("usecomboe").GetValue<bool>(),
                         _menu.Item("usecombor").GetValue<bool>());
                }

                if (_menu.Item("grabkey").GetValue<KeyBind>().Active)
                {
                    Combo(true, false, false);
                }
            }
        }

        private static void AutoCast(bool dashing, bool immobile)
        {
            if (_q.IsReady())
            {
                foreach (
                    var ii in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(x => x.IsValidTarget(_menu.Item("maxdist").GetValue<Slider>().Value)))
                {
                    if (dashing && _menu.Item("dograb" + ii.ChampionName).GetValue<StringList>().SelectedIndex != 0)
                        if (ii.Distance(Me.ServerPosition) > _menu.Item("mindist").GetValue<Slider>().Value)
                            _q.CastIfHitchanceEquals(ii, HitChance.Dashing);

                    if (immobile && _menu.Item("dograb" + ii.ChampionName).GetValue<StringList>().SelectedIndex != 0)
                        if (ii.Distance(Me.ServerPosition) > _menu.Item("mindist").GetValue<Slider>().Value)
                            _q.CastIfHitchanceEquals(ii, HitChance.Immobile);
                }
            }
        }

        private static void Combo(bool useq, bool usee, bool user)
        {
            if (useq && _q.IsReady())
            {
                var qtarget = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
                if (qtarget.IsValidTarget(_menu.Item("maxdist").GetValue<Slider>().Value))
                {
                    var poutput = _q.GetPrediction(qtarget);
                    if (poutput.Hitchance >= (HitChance) _menu.Item("hitchanceq").GetValue<Slider>().Value + 2)
                    {
                        if (qtarget.Distance(Me.ServerPosition) > _menu.Item("mindist").GetValue<Slider>().Value)
                        {
                            if (_menu.Item("dograb" + qtarget.ChampionName).GetValue<StringList>().SelectedIndex != 0)
                            {
                                if (_menu.Item("dograb" + qtarget.ChampionName).GetValue<StringList>().SelectedIndex == 2)
                                {
                                    if (poutput.CollisionObjects.All(
                                            x => x.Health <= _r.GetDamage(x) && x.Distance(Me.ServerPosition) <= _r.Range))
                                    {
                                        _r.Cast();
                                        Utility.DelayAction.Add(150, () => _x.Cast(poutput.CastPosition));
                                    }
                                }

                                _q.Cast(poutput.CastPosition);
                            }
                        }
                    }
                }
            }

            if (usee && _e.IsReady())
            {
                var etarget = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);
                if (etarget.IsValidTarget(_menu.Item("mindist").GetValue<Slider>().Value + etarget.BoundingRadius))
                {
                    _e.CastOnUnit(Me);
                }

                var qtarget = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
                if (qtarget.IsValidTarget(_menu.Item("maxdist").GetValue<Slider>().Value))
                {
                    if (qtarget.HasBuff("rocketgrab2", true))
                    {
                        _e.CastOnUnit(Me);
                    }
                }
            }

            if (user && _r.IsReady())
            {
                var rtarget = TargetSelector.GetTarget(_r.Range, TargetSelector.DamageType.Magical);
                if (rtarget.IsValidTarget())
                {
                    if (!_e.IsReady() && rtarget.HasBuffOfType(BuffType.Knockup))
                    {
                        if (rtarget.Health > rtarget.GetSpellDamage(rtarget, SpellSlot.R))
                        {
                            _r.Cast();
                        }
                    }
                }
            }
        }

        private static void Secure(bool useq, bool usee, bool user)
        {
            if (user && _r.IsReady())
            {
                var rtarget = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(h => h.IsEnemy);
                if (rtarget.IsValidTarget(_r.Range))
                {
                    if (Me.GetSpellDamage(rtarget, SpellSlot.R) >= rtarget.Health)
                        _r.Cast();
                }
            }

            if (usee && _e.IsReady())
            {
                var etarget = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(h => h.IsEnemy);
                if (etarget.IsValidTarget(_e.Range))
                {
                    if (Me.GetSpellDamage(etarget, SpellSlot.E) >= etarget.Health)
                        _e.CastOnUnit(Me);
                }
            }

            if (useq && _q.IsReady())
            {
                var qtarget = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(h => h.IsEnemy);
                if (qtarget.IsValidTarget(_menu.Item("maxdist").GetValue<Slider>().Value))
                {
                    if (Me.GetSpellDamage(qtarget, SpellSlot.Q) >= qtarget.Health)
                    {
                        var poutput = _q.GetPrediction(qtarget);
                        if (poutput.Hitchance >= HitChance.Medium)
                        {
                            if (qtarget.Distance(Me.ServerPosition) > 
                                _menu.Item("mindist").GetValue<Slider>().Value)
                            {
                                _q.Cast(poutput.CastPosition);
                            }
                        }
                    }
                }
            }
        }
    }
}