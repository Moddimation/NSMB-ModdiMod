using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UI;

namespace NSMB.UI.Elements {
    public class GIF : MonoBehaviour {

        //---Serialized Variables
        [SerializeField] private bool repeat = true;
        [SerializeField] private bool reverse = false;
        [SerializeField] private float latency = 0.1f;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private GIF next;

        //---Private Variables
        private int index = 0;
        private bool isStop = false;
        private Image image;

        private void Iter() {
            image.sprite = frames[index];
            index = reverse ? index - 1 : index + 1;
        }

        private IEnumerator PlayLoop() {
            while (!isStop) {
                Iter();

                if (index < 0) {
                    index = frames.Length - 1;
                }
                if (index >= frames.Length) {
                    index = 0;
                }

                yield return new WaitForSeconds(latency);
            }
        }

        private IEnumerator PlayChain() {
            while (!isStop) {
                Iter();

                if ((index < 0) || (index >= frames.Length)) {
                    if (next != null) next.enabled = true;
                    Stop();
                    yield return null;
                } else {
                    yield return new WaitForSeconds(latency);
                }
            }
        }
        private IEnumerator PlayOnce() {
            while (!isStop) {
                Iter();

                if ((index < 0) || (index >= frames.Length)) {
                    Stop();
                    yield return null;
                } else {
                    yield return new WaitForSeconds(latency);
                }
            }
        }

        public void Stop() {
            isStop = true;
            this.enabled = false;
            StopAllCoroutines();  // <--- Stop coroutines on Stop()
        }
        public void Play() {
            StopAllCoroutines();  // <--- Stop any running coroutine before starting new one
            isStop = false;
            index = 0;

            if (repeat == false) {
                StartCoroutine(PlayOnce());
            } else if (next == null) {
                StartCoroutine(PlayLoop());
            } else {
                StartCoroutine(PlayChain());
            }
        }

        private void Init() {
            if (image == null) {
                image = gameObject.GetComponent<Image>();

                if (image == null) {
                    Debug.LogError("GIF NEEDS IMAGE COMPONENT");
                    return;
                }
            }

            Play();
        }

        void Start() {
            Init();
        }
        void OnEnable() {
            Init();
        }
    }
}
