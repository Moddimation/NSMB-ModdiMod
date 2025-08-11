using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace NSMB.UI.MainMenu.Submenus.InRoom {
    public class CharacterButton : MonoBehaviour, ISelectHandler {

        //---Public Variables
        public CharacterAsset chara;
        public Image image;

        //---Serialized Variables
        [SerializeField] private TMP_Text colorNameString;

        public void Instantiate() {
            if (chara == null) {
                if(image != null)
                    Destroy(image.gameObject);
                return;
            }
        }

        public void OnSelect(BaseEventData eventData) {
            UpdateLabel();
        }

        public void OnPress() {
            UpdateLabel();
        }

        private void UpdateLabel() {
            colorNameString.text = GlobalController.Instance.translationManager.GetTranslation(chara ? chara.TranslationString : "skin.default");
        }
    }
}
