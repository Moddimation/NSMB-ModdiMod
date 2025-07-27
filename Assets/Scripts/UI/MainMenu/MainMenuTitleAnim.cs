using UnityEngine;
using JimmysUnityUtilities;

namespace NSMB.UI.MainMenu {
    public class MainMenuTitleAnim : MonoBehaviour {

        [SerializeField] private RectTransform titleMain, titleSub;
        [SerializeField] private UI.Elements.GIF titleGif;
        [SerializeField] private float latency = 6f;
        [SerializeField] private float latencySub = 820f;
        [SerializeField] private float waitSub = 1.4f;

        private float waitTimer = 0.0f;
        private int state = 0;

        void Awake() {
               SetMainX(-1024f);
               state = 0;
               SetSubY(100);
        }

        void Update() {
               if (state == 0) {
                      AnimMain();
               } else if (state == 1) {
                      AnimSub();
               } else if (state == 2) {
                      enabled = false;
               }
        }

        private void AnimMain() {
            float mainSpeed = 1024f * latency / 3.5f;
            var pos = titleMain.anchoredPosition;
            pos.x = Mathf.MoveTowards(pos.x, 0f, mainSpeed * Time.deltaTime);
            titleMain.anchoredPosition = pos;

            if (pos.x == 0f) {
                titleGif.enabled = true;
                FindFirstObjectByType<Sound.LoopingMusicPlayer>().Restart();
                waitTimer = waitSub;
                state = 1;
            }
        }

        private void AnimSub() {
            if (waitTimer > 0f) {
                waitTimer -= Time.deltaTime;
                return;
            }

            var pos = titleSub.anchoredPosition;
            pos.y = Mathf.MoveTowards(pos.y, 0f, latencySub * Time.deltaTime);
            titleSub.anchoredPosition = pos;

            if (pos.y == 0f) {
                state = 2;
            }
        }

        private void SetSubY(float y) {
               titleSub.anchoredPosition = new Vector2(titleSub.anchoredPosition.x, y);
        }
        private void SetMainX(float x) {
               titleMain.anchoredPosition = new Vector2(x, titleSub.anchoredPosition.y);
        }
    }
}
