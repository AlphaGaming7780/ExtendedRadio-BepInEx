using System;
using System.Collections;
using System.IO;
using Colossal.UI.Binding;
using Game.SceneFlow;
using Game.UI;
using UnityEngine;

namespace ExtendedRadio
{
	
	class ExtendedRadioUI : UISystemBase
	{	

		private GetterValueBinding<bool> customnetworkui;
		private GetterValueBinding<bool> DisableAdsOnStartup;

        protected override void OnCreate() {

			base.OnCreate();

            // AddBinding(new TriggerBinding("extended_radio", "open_settings", new Action(SettingsButtonCallBack)));
			AddBinding(customnetworkui = new GetterValueBinding<bool>("extended_radio_settings", "customnetworkui", () => Settings.customNetworkUI));
			AddBinding(new TriggerBinding<bool>("extended_radio_settings", "customnetworkui", new Action<bool>(UpdateSettings_customNetworkUi)));
			
			AddBinding(DisableAdsOnStartup = new GetterValueBinding<bool>("extended_radio_settings", "DisableAdsOnStartup", () => Settings.DisableAdsOnStartup));
			AddBinding(new TriggerBinding<bool>("extended_radio_settings", "DisableAdsOnStartup", new Action<bool>(UpdateSettings_disableAdsOnStartup)));

			AddBinding(new TriggerBinding("extended_radio", "reloadradio", new Action(ReloadRadio)));
        }

		private void UpdateSettings_customNetworkUi(bool newValue) {
			Settings.customNetworkUI = newValue;
			Settings.SaveSettings();
			customnetworkui.Update();
		}

		private void UpdateSettings_disableAdsOnStartup(bool newValue) {
			Settings.DisableAdsOnStartup = newValue;
			Settings.SaveSettings();
			DisableAdsOnStartup.Update();
		}

		private void ReloadRadio() {
			ExtendedRadio.radio.Reload(true);
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