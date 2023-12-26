// using System.Collections;
// using Game.SceneFlow;
// using UnityEngine;

// namespace ExtendedRadio 
// {
// 	internal class ExtendedRadioUi : MonoBehaviour
// 	{
// 		internal void ChangeUiNextFrame(string js) {
// 			StartCoroutine(ChangeUI(js));
// 		}

// 		private IEnumerator ChangeUI(string js) {
// 			yield return new WaitForEndOfFrame();
// 			GameManager.instance.userInterface.view.View.ExecuteScript(js);
// 			yield return null;
// 		}
// 	}
// }