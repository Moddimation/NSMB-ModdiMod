using JimmysUnityUtilities;
using NSMB.Utilities.Extensions;
using Quantum;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NSMB.UI.Intro {
    public class IntroManager : MonoBehaviour {

        //---Serialized Variables
        [SerializeField] private GameObject others;
        [SerializeField] private Image fullscreenImage, logo, sublogo;
        [SerializeField] private AudioSource sfx, sfxPan;
        [SerializeField] private Color  fadeColor;
        [SerializeField] private float appearDiff = 0.05f;
        [SerializeField] private float appearwait = 0.1f;
        [SerializeField] private float unhideDiff = 0.1f;
        [SerializeField] private float hideDiff = 0.43f;

        public void Start() {
            StartCoroutine(IntroSequence());
        }

        public void PlayRandomCharacterSound() {
            sfxPan.Play();
            sfx.Stop();
        }

        private IEnumerator LogoColor() {
            for (float a = 0; a <= 1; a += appearDiff) {
                Color c = new Color(1f, 1f, 1f, a);
                logo.color = c;
                sublogo.color = c;

                yield return new WaitForSeconds(appearwait);
            }
        }

        private IEnumerator IntroSequence() {
            yield return new WaitForSeconds(0.5f);
            yield return FadeImageToValue(fullscreenImage, 0, unhideDiff);
            sfx.Play();
            yield return new WaitForSeconds(0.2f);
            yield return LogoColor();
            yield return new WaitForSeconds(4f);
            sfxPan.Play();

#if !DISABLE_SCENE_CHANGE
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
            sceneLoad.allowSceneActivation = false;
#endif

            yield return new WaitForSeconds(0.25f);
            fullscreenImage.color = fadeColor;
            yield return FadeImageToValue(fullscreenImage, 1, hideDiff);

            EventSystem.current.gameObject.SetActive(false);

            yield return new WaitForSeconds(1f);

#if !DISABLE_SCENE_CHANGE
            while (sceneLoad.progress < 0.9f) {
                yield return null;
            }
#endif
#if !DISABLE_SCENE_CHANGE
            sceneLoad.allowSceneActivation = true;
            while (!sceneLoad.isDone) {
                yield return null;
            }
            others.SetActive(false);

            // Fuck this lag spike man
            yield return new WaitForSeconds(0.1f);
            do {
                yield return null;
            } while (Time.deltaTime >= Time.maximumDeltaTime);

            yield return FadeImageToValue(fullscreenImage, 0, 0.33f);
            yield return new WaitForSeconds(0.5f);
            SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
#endif
        }

        private static IEnumerator FadeImageToValue(Image image, float newAlpha, float time) {
            float remainingTime = time;
            float startingAlpha = image.color.a;

            Color newColor = image.color;
            while ((remainingTime -= Time.deltaTime) > 0) {
                newColor.a = Mathf.Lerp(startingAlpha, newAlpha, 1f - (remainingTime / time));
                image.color = newColor;
                yield return null;
            }

            newColor.a = newAlpha;
            image.color = newColor;
        }
    }
}
