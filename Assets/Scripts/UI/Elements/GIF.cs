using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UI;

namespace NSMB.UI.Elements {
    public class GIF : MonoBehaviour {

        //---Serialized Variables
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
                Debug.Log("00");
                Iter();

                Debug.Log("0");

                if ((index < 0) || (index >= frames.Length)) {
                    next.enabled = true;
                    isStop = true;
                    Debug.Log("1");
                } else {
                    Debug.Log("2");
                    yield return new WaitForSeconds(latency);
                }
                Debug.Log("3");
            }
        }

        public void Stop() {
            isStop = true;
        }
        public void Play() {
            isStop = false;
            index = 0;

            Debug.Log($"next : {next}");

            if (next == null) {
                StartCoroutine(PlayLoop());
            } else {
                StartCoroutine(PlayChain());
            }
        }

        private void Init() {
            image = gameObject.GetComponent<Image>();

            if (image == null) {
                Debug.LogError("GIF NEEDS IMAGE COMPONENT");
            } else {
                Play();
            }
        }

        void Start() {
            Init();
        }
        void OnEnable() {
            Init();
        }
    }
}
