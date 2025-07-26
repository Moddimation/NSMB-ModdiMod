using UnityEngine;
using System.Collections;
using JimmysUnityUtilities;

namespace NSMB.UI.MainMenu {
    public class MenuMenuTitleAnim : MonoBehaviour {

        [SerializeField] private RectTransform titleMain, titleSub;
        [SerializeField] private UI.Elements.GIF titleGif;
        [SerializeField] private float latency = 0.00008f;
        private float temp = 0.0f;
        IEnumerator AnimMain() {
            while (temp < 0.5f) {
                titleMain.SetPivotX(temp);
                Debug.Log($"VAL: {temp}, {titleMain.pivot}");

                temp += (latency* Time.deltaTime);

                yield return new WaitForSeconds(0.1f);
            }
        }
        IEnumerator AnimSub() {
            titleSub.SetPivotY(-2f);

            yield return null;
        }
        IEnumerator AnimRoot() {
            temp = 3.0f;
            yield return AnimMain();

            yield return new WaitForSeconds(0.2f);

            yield return AnimSub();

            yield return null;
        }
        void Awake() {
            StartCoroutine(AnimRoot());
        }
    }
}