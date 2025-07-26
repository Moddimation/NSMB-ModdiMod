using UnityEngine;
using JimmysUnityUtilities;

namespace NSMB.UI.MainMenu {
    public class MainMenuTitleAnim : MonoBehaviour {

        [SerializeField] private RectTransform titleMain, titleSub;
        [SerializeField] private UI.Elements.GIF titleGif;
        [SerializeField] private float latency = 6f;
        [SerializeField] private float latencySub = 800;

        private float temp = 0.0f;
        private float waitTimer = 0.0f;
        private int state = 0;

        void Awake() {
            temp = -3.0f;
            state = 0;
            SetSubY(100);
        }

        void Update() {
            if(waitTimer > 0f) {
                waitTimer -= Time.deltaTime;
                return;
            }

            if (state == 0) {
                AnimMain();
            } else if (state == 1) {
                AnimSub();
            } else if (state == 2) {
                enabled = false;
            }
        }

        private void AnimMain() {
            if (temp < 0.5f) {
                titleMain.SetPivotX(temp);

                temp += latency * Time.deltaTime;
            } else {
                titleGif.enabled = true;
                waitTimer = 1f;
                temp = 100;
                state = 1;
            }
        }

        private void AnimSub() {
            if (temp > 0) {
                SetSubY(temp);

                temp -= latencySub * Time.deltaTime;
            } else {
                state = 2;
            }
        }
        private void SetSubY(float y) {
            titleSub.anchoredPosition = new Vector2(titleSub.anchoredPosition.x, y);
        }
    }
}
