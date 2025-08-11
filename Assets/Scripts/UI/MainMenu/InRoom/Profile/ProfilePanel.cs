using NSMB.Networking;
using NSMB.UI.Elements;
using Quantum;
using UnityEngine;
using UnityEngine.UI;

namespace NSMB.UI.MainMenu.Submenus.InRoom {
    public class ProfilePanel : InRoomSubmenuPanel {

        //---Properties
        public override bool IsInSubmenu => teamChooser.content.activeSelf || paletteChooser.content.activeSelf;

        //---Serialized Variables
        [SerializeField] private Image paletteBackground;
        public PaletteChooser paletteChooser;
        public CharacterChooser charaChooser;
        [SerializeField] private TeamChooser teamChooser;
        [SerializeField] private SpriteChangingToggle spectateToggle;

        public override void Initialize() {
            paletteChooser.Initialize();
            charaChooser.Initialize();
            teamChooser.Initialize();

            QuantumEvent.Subscribe<EventPlayerDataChanged>(this, OnPlayerDataChanged);
        }

        public override bool TryGoBack(out bool playSound) {
            if (teamChooser.content.activeSelf) {
                teamChooser.Close(true);
                playSound = false;
                return false;
            }

            if (paletteChooser.content.activeSelf) {
                paletteChooser.Close(true);
                playSound = false;
                return false;
            }

            if (charaChooser.content.activeSelf) {
                charaChooser.Close(true);
                playSound = false;
                return false;
            }

            return base.TryGoBack(out playSound);
        }

        public void OnSpectateToggled() {
            QuantumGame game = NetworkHandler.Runner.Game;
            foreach (var slot in game.GetLocalPlayerSlots()) {
                game.SendCommand(slot, new CommandChangePlayerData {
                    EnabledChanges = CommandChangePlayerData.Changes.Spectating,
                    Spectating = spectateToggle.isOn,
                });
            }
            menu.Canvas.PlayConfirmSound();
        }

        private void SetPaletteButtonState(int index) {
            paletteChooser.ChangePaletteButton(index);
        }
        private void SetCharacterButtonState(Frame f, int index, bool sound) {
            charaChooser.ChangeCharaButton(f, index, sound);
        }

        //---Callbacks
        private unsafe void OnPlayerDataChanged(EventPlayerDataChanged e) {
            if (!e.Game.PlayerIsLocal(e.Player)) {
                return;
            }

            Frame f = e.Game.Frames.Predicted;

            // Set character button to the correct state
            PlayerData* data = QuantumUtils.GetPlayerData(f, e.Player);
            SetPaletteButtonState(data->Palette);
            SetCharacterButtonState(f, data->Character, false);
            spectateToggle.SetIsOnWithoutNotify(data->ManualSpectator);
        }
    }
}
