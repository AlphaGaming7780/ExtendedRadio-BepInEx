using System;
using System.Collections;
using System.Dynamic;
using System.IO;
using Colossal.Mono.Cecil.Cil;
using Colossal.UI.Binding;
using Game.SceneFlow;
using Game.UI;
using UnityEngine;

namespace ExtendedRadio
{
	
	class ExtendedRadioUI : UISystemBase
	{	

		private GetterValueBinding<bool> customnetworkui;

        protected override void OnCreate() {

			base.OnCreate();

            // AddBinding(new TriggerBinding("extended_radio", "open_settings", new Action(SettingsButtonCallBack)));
			AddBinding(customnetworkui = new GetterValueBinding<bool>("extended_radio_settings", "customnetworkuigetvalue", () => Settings.customNetworkUI));
			AddBinding(new TriggerBinding<bool>("extended_radio_settings", "customnetworkuisendvalue", new Action<bool>(UpdateSettings_customNetworkUi)));
			
        }

		private void UpdateSettings_customNetworkUi(bool newValue) {
			Settings.customNetworkUI = newValue;
			Settings.SaveSettings();
			customnetworkui.Update();
		}

		private void SettingsButtonCallBack() {
			Debug.Log("YEAH");
		}

		internal static string GetStringFromEmbbededJSFile(string path) {
			return new StreamReader(ExtendedRadio.GetEmbedded("UI."+path)).ReadToEnd();
		}

	}

	internal class ExtendedRadioUI_Mono : MonoBehaviour
	{
		internal void ChangeUiNextFrame(string js) {
			StartCoroutine(ChangeUI(js));
		}

		private IEnumerator ChangeUI(string js) {
			yield return new WaitForEndOfFrame();
			GameManager.instance.userInterface.view.View.ExecuteScript(js);
			yield return null;
		}
	}
}