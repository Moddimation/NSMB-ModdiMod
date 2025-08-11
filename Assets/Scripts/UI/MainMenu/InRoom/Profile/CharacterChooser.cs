using NSMB.Networking;
using NSMB.UI.Elements;
using Quantum;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Navigation = UnityEngine.UI.Navigation;

namespace NSMB.UI.MainMenu.Submenus.InRoom {
    public class CharacterChooser : MonoBehaviour, KeepChildInFocus.IFocusIgnore {

        //---Serialized Variables
        // [SerializeField] private SimulationConfig config;
        [SerializeField] private MainMenuCanvas canvas;
        [SerializeField] private GameObject template, blockerTemplate;
        [SerializeField] public GameObject content;
        [SerializeField] private Sprite clearSprite;
        [SerializeField] private GameObject selectOnClose;
        [SerializeField] private ProfilePanel profilePanel;

        [SerializeField] private Image baseImage;

        //---Private Variables
        private readonly List<CharacterButton> charaButtons = new();
        private readonly List<Button> buttons = new();
        private readonly List<Navigation> navigations = new();
        private GameObject blocker;
        private CharacterAsset character;
        private int selected;
        private bool initialized;
        private int currentCharacterIndex;

        public void OnDisable() {
            Close(false);
        }

        public unsafe void Initialize() {
            if (initialized) {
                return;
            }

            if(profilePanel == null)
            {
                Debug.Log("ERROR: DID NOT SPECIFY PROFILE PANEL IN CharacterChooser! MainMenu->RoundedRectMask->InRoom.->ProfilePanel->ProfileSettings->CharacterNew.");
            }

            AssetRef<CharacterAsset>[] charas = GlobalController.Instance.config.CharacterDatas;

            for (int i = 0; i < charas.Length; i++) {
                CharacterAsset chara = QuantumUnityDB.GetGlobalAsset(charas[i]);

                GameObject newButton = Instantiate(template, template.transform.parent);
                CharacterButton cb = newButton.GetComponent<CharacterButton>();
                charaButtons.Add(cb);
                cb.chara = chara;

                Button b = newButton.GetComponent<Button>();
                newButton.name = chara ? chara.TranslationString : "Reset";
                if (!chara) {
                    b.image.sprite = clearSprite;
                }
                b.image.sprite = chara.IconSprite;

                newButton.SetActive(true);
                buttons.Add(b);

                Navigation navigation = new() { mode = Navigation.Mode.Explicit };

                if (i > 0 && i % 4 != 0) {
                    Navigation n = navigations[i - 1];
                    n.selectOnRight = b;
                    navigations[i - 1] = n;
                    navigation.selectOnLeft = buttons[i - 1];
                }
                if (i >= 4) {
                    Navigation n = navigations[i - 4];
                    n.selectOnDown = b;
                    navigations[i - 4] = n;
                    navigation.selectOnUp = buttons[i - 4];
                }

                navigations.Add(navigation);
            }

            for (int i = 0; i < buttons.Count; i++) {
                buttons[i].navigation = navigations[i];
            }
            initialized = true;

            foreach (CharacterButton c in charaButtons) {
                c.Instantiate();
            }
        }

        public void SelectChara(Button button) {
            int newIndex = buttons.IndexOf(button);
            var game = NetworkHandler.Runner.Game;
            foreach (var slot in game.GetLocalPlayerSlots()) {
                game.SendCommand(slot, new CommandChangePlayerData { 
                    EnabledChanges = CommandChangePlayerData.Changes.Character,
                    Character = (byte) newIndex,
                });
            }
            
            Close(false);
            ChangeCharaButton(game.Frames.Predicted, newIndex, true);
        }

        public void ChangeCharaButton(Frame f, int index, bool sound) {
            bool changed = selected != index;
/*
            for (int i = 0; i < characterButtonImages.Length; i++) {
                var image = characterButtonImages[i];
                image.sprite = disabledCharacterButtonSprites[i];

                if (i < characterButtonLogos.Length && characterButtonLogos[i]) {
                    characterButtonLogos[i].color = disabledCharacterButtonLogoColor;
                }
            }

            characterButtonImages[index].sprite = enabledCharacterButtonSprites[index];
            paletteBackground.sprite = disabledCharacterButtonSprites[index];
            if (index < characterButtonLogos.Length && characterButtonLogos[index]) {
                characterButtonLogos[index].color = enabledCharacterButtonLogoColor;
            }
*/
            selected = index;
            var allCharacters = f.SimulationConfig.CharacterDatas;
            CharacterAsset chara = null;

            if (index >= 0 && index < allCharacters.Length) {
                chara = QuantumUnityDB.GetGlobalAsset(allCharacters[index]);
            }

            if (chara) {
                baseImage.sprite = chara.IconSprite;
            } else {
                baseImage.sprite = clearSprite;
            }

            CharacterAsset characterAsset = f.FindAsset(allCharacters[Mathf.Clamp(index, 0, allCharacters.Length)]);
            profilePanel.paletteChooser.ChangeCharacter(characterAsset);

            if (changed) {
                Settings.Instance.generalCharacter = index;
                Settings.Instance.SaveSettings();
            }

            if (sound && changed) {
                profilePanel.menu.Canvas.PlaySound(SoundEffect.Player_Voice_Selected, characterAsset);
            }
        }

        public void Open() {
            Initialize();

            blocker = Instantiate(blockerTemplate, canvas.transform);
            blocker.SetActive(true);
            content.SetActive(true);
            canvas.PlayCursorSound();

            EventSystem.current.SetSelectedGameObject(buttons[selected].gameObject);
        }

        public void Close(bool playSound) {
            Destroy(blocker);
            EventSystem.current.SetSelectedGameObject(selectOnClose);
            content.SetActive(false);

            if (playSound) {
                canvas.PlaySound(SoundEffect.UI_Back);
            }
        }
    }
}
