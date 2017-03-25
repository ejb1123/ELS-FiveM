﻿using CitizenFX.Core;
using CitizenFX.Core.Native;
using ELS.Siren.Tones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CitizenFX.Core.UI;
using ELS.configuration;

namespace ELS.Siren
{
    public delegate void StateChangedHandler(Tone tone);

    /// <summary>
    /// Has diffrent tones
    /// </summary>
    class Siren : IManagerEntry
    {
        private configuration.ControlConfiguration.ELSControls keyBinding;
        public Vehicle _vehicle { get; set; }
        private MainSiren _mainSiren;
        configuration.ControlConfiguration.ELSControls keybindings = new configuration.ControlConfiguration.ELSControls();

        internal struct Tones
        {
            public Tone horn;
            public Tone tone1;
            public Tone tone2;
            public Tone tone3;
            public Tone tone4;
        }
        Tones _tones;

        internal class MainSiren
        {
            public bool _state { get; private set; }
            private Tone currentTone;
            private List<Tone> MainTones;
            public MainSiren(Tones tonesl)
            {
                MainTones = new List<Tone>(new[] { tonesl.tone1, tonesl.tone2, tonesl.tone3, tonesl.tone4 });
                currentTone = MainTones[0];
            }

            public void SetState(bool state)
            {
                _state = state;
                if (_state)
                {
                    _state = true;
                    foreach (var siren in MainTones)
                    {
                        if (siren._state == true) { siren.SetState(false);}
                    }
                    currentTone.SetState(true);
                }
                else
                {
                    _state = false;
                    currentTone.SetState(false);
                    currentTone = MainTones[0];
                }
            }
            internal void nextTone()
            {
                
                currentTone.SetState(false);
                currentTone = MainTones[MainTones.IndexOf(currentTone) + 1];
                currentTone.SetState(true);
                
            }

            internal void previousTone()
            {
                currentTone.SetState(false);
                currentTone = MainTones[MainTones.IndexOf(currentTone) -1];
                currentTone.SetState(true);
            }

        }
        public Siren(Vehicle vehicle)
        {
            _vehicle = vehicle;
            Debug.WriteLine($"horn:{VCF.data.SOUNDS.MainHorn.AudioString}");
            _tones = new Tones
            {
                horn = new Tone(VCF.data.SOUNDS.MainHorn.AudioString, _vehicle, ToneType.Horn),
                tone1 = new Tone(VCF.data.SOUNDS.SrnTone1.AudioString, _vehicle, ToneType.SrnTon1),
                tone2 = new Tone(VCF.data.SOUNDS.SrnTone2.AudioString, _vehicle, ToneType.SrnTon2),
                tone3 = new Tone(VCF.data.SOUNDS.SrnTone3.AudioString, _vehicle, ToneType.SrnTon3),
                tone4 = new Tone(VCF.data.SOUNDS.SrnTone4.AudioString, _vehicle, ToneType.SrnTon4)
            };
            _mainSiren = new MainSiren(_tones);

        }
        public void CleanUP()
        {
            _tones.horn.CleanUp();
            _tones.tone1.CleanUp();
            _tones.tone2.CleanUp();
            _tones.tone3.CleanUp();
            _tones.tone4.CleanUp();

        }
        public void ticker()
        {
            Game.DisableControlThisFrame(0, Control.VehicleHorn);
            if (Game.IsControlJustReleased(0, Control.VehicleHorn))
            {
                Function.Call(Hash.DISABLE_VEHICLE_IMPACT_EXPLOSION_ACTIVATION, _vehicle, true);
                _vehicle.IsSirenActive = !_vehicle.IsSirenActive;
            }

            if ((Game.IsControlJustPressed(0, configuration.ControlConfiguration.KeyBindings.Sound_Ahorn) && Game.CurrentInputMode == InputMode.MouseAndKeyboard) ||
            (Game.IsControlJustPressed(2, Control.ScriptPadDown) && Game.CurrentInputMode == InputMode.GamePad))
            {
                Game.DisableControlThisFrame(0, configuration.ControlConfiguration.KeyBindings.Sound_Ahorn);
                Game.DisableControlThisFrame(2, Control.ScriptPadDown);
                _tones.horn.SetState(true);
            }
            if ((Game.IsControlJustReleased(0, configuration.ControlConfiguration.KeyBindings.Sound_Ahorn) && Game.CurrentInputMode == InputMode.MouseAndKeyboard)
                || (Game.IsControlJustReleased(2, Control.ScriptPadDown) && Game.CurrentInputMode == InputMode.GamePad))
            {
                Game.DisableControlThisFrame(0, configuration.ControlConfiguration.KeyBindings.Sound_Ahorn);
                Game.DisableControlThisFrame(2, Control.ScriptPadDown);
                _tones.horn.SetState(false);
            }

            if (Game.IsControlJustReleased(0, configuration.ControlConfiguration.KeyBindings.Snd_SrnTon1))
            {
                if (_tones.tone1._state == true && _mainSiren._state==true)
                {
                    _mainSiren.SetState(false);
                    _tones.tone1.SetState(false);
                }
                else
                {
                    _tones.tone1.SetState(!_tones.tone1._state);
                }
            }


            if (Game.IsControlJustReleased(0, configuration.ControlConfiguration.KeyBindings.Snd_SrnTon2))
            {
                if (_tones.tone2._state == true && _mainSiren._state==true)
                {
                    _mainSiren.SetState(false);
                    _tones.tone2.SetState(false);
                }
                else
                {
                    _tones.tone2.SetState(!_tones.tone2._state);
                }
            }


            if (Game.IsControlJustReleased(0, configuration.ControlConfiguration.KeyBindings.Snd_SrnTon3))
            {
                _tones.tone3.SetState(!_tones.tone3._state);
            }

            if (Game.IsControlJustReleased(0, configuration.ControlConfiguration.KeyBindings.Snd_SrnTon4))
            {
                _tones.tone4.SetState(!_tones.tone4._state);
            }


            if (Game.IsControlJustPressed(0, configuration.ControlConfiguration.KeyBindings.Sound_Manul))
            {
                if (!_mainSiren._state)
                {
                    _tones.tone1.SetState(true);
                }
                else
                {
                    _mainSiren.nextTone();
                }
            }
            if (Game.IsControlJustReleased(0, configuration.ControlConfiguration.KeyBindings.Sound_Manul))
            {
                if (!_mainSiren._state)
                {
                    _tones.tone1.SetState(false);
                }
                else
                {
                    _mainSiren.previousTone();
                }
            }

            if (Game.IsControlJustReleased(0, configuration.ControlConfiguration.KeyBindings.Toggle_SIRN))
            {
                _mainSiren.SetState(!_mainSiren._state);
            }
        }

        public void updateLocalRemoteSiren(string sirenString, bool state)
        {
            Debug.WriteLine(sirenString);
            Enum.TryParse(sirenString, out ToneType tonetype);
            switch (tonetype)
            {
                case ToneType.Horn:
                    _tones.horn.SetRemoteState(state);
                    Debug.WriteLine("setting horn" + state);
                    break;
                case ToneType.SrnTon1:
                    _tones.tone1.SetRemoteState(state);
                    Debug.WriteLine("setting 1");
                    break;
                case ToneType.SrnTon2:
                    _tones.tone2.SetRemoteState(state);
                    Debug.WriteLine("setting 2");
                    break;
                case ToneType.SrnTon3:
                    _tones.tone3.SetRemoteState(state);
                    Debug.WriteLine("setting 3");
                    break;
                case ToneType.SrnTon4:
                    _tones.tone4.SetRemoteState(state);
                    Debug.WriteLine("setting 4");
                    break;
            }
        }

    }
}