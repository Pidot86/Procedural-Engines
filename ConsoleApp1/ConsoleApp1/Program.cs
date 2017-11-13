using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/*
 * Engine types:
 * Early | Mid | Modern
 * 
 * Gas generator 300s|310s|320s cheapish, light, strong, midrange ATMisp
 * Expander cycle 330s|345s|360s cheap, weak, lowATMisp
 * Staged combustion 310s|320s|330s betterATMisp, strong, expensive
 * Closed staged combustion 320s|330s|340s more expensive, better isp overall
 * Electric pump 320s|330s|340s cheapish, uses power, low thrust
 * Tap off 300s|310s|320s midprice, good ATMisp, mid thrust
 * 
 * */

namespace ConsoleApp1 {

	public class PEngine : PartModule, IPartCostModifier, IPartMassModifier {

		/*
		 * 
		 * Level cost reduction:
		 * 
		 * 1200, 2400, 4800
		 * LVL1: 1200
		 * LVL2: 900, 1800
		 * LVL3: 600, 1200, 2400
		 * 
		 * */

		public static readonly string[] engineName = {
			"Gas Generator",
			"Expander Cycle",
			"Staged Combustion",
			"Closed Staged Combustion",
			"Electric pump",
			"Tap off"
		};

		public static readonly float[] engineBaseMass = {
			1.4f, 1.3f, 1.2f,
			0.6f, 0.5f, 0.4f,
			1.7f, 1.55f, 1.4f,
			1.8f, 1.6f, 1.4f,
			1.0f, 0.9f, 0.8f,
			0.9f, 0.8f, 0.7f
		};

		public static readonly float[] engineBaseCost = {
			1200.0f, 2400.0f, 4800.0f,
			600.0f, 1200.0f, 2400.0f,
			2000.0f, 4000.0f, 8000.0f,
			3000.0f, 6000.0f, 12000.0f,
			900.0f, 1800.0f, 3600.0f,
			1800.0f, 3600.0f, 7200.0f
		};

		public static readonly float[] engineBaseVISP = {
			300.0f, 310.0f, 320.0f,
			330.0f, 345.0f, 360.0f,
			310.0f, 320.0f, 330.0f,
			320.0f, 330.0f, 340.0f,
			320.0f, 330.0f, 340.0f,
			300.0f, 310.0f, 320.0f
		};

		public static readonly float[] engineBaseAISP = {
			250.0f, 260.0f, 270.0f,
			180.0f, 200.0f, 220.0f,
			270.0f, 280.0f, 290.0f,
			290.0f, 300.0f, 310.0f,
			280.0f, 290.0f, 300.0f,
			270.0f, 280.0f, 290.0f
		};

		public static readonly float[] engineBaseThrust = {
			200.0f, 230.0f, 260.0f,
			40.0f, 55.0f, 70.0f,
			240.0f, 280.0f, 320.0f,
			260.0f, 300.0f, 340.0f,
			90.0f, 120.0f, 150.0f,
			160.0f, 190.0f, 220.0f
		};

		public readonly int[] upgradeLevels = {
			isNU ("basicRocketry") + isNU ("generalRocketry") + isNU ("advRocketry"),
			isNU ("advRocketry") + isNU ("propulsionSystems") + isNU ("precisionPropulsion"),
			isNU ("heavyRocketry") + isNU ("heavierRocketry") + isNU ("veryHeavyRocketry"),
			isNU ("advFuelSystems") + isNU ("largeVolumeContainment") + isNU ("highPerformanceFuelSystems"),
			isNU ("advElectrics") + isNU ("largeElectrics") + isNU ("specializedElectrics"),
			isNU ("advRocketry") + isNU ("fuelSystems") + isNU ("advFuelSystems")
		};

		static private int isNU (string a) {
			return isNodeUnlocked (a);
		}

		static private int isNodeUnlocked (string a) {
			return (ResearchAndDevelopment.GetTechnologyState (a) == RDTech.State.Available ? 1 : 0);
		}

		//-=-=-=

		[KSPField (isPersistant = true, guiActive = false)]
		float Cost;

		[KSPField (isPersistant = true, guiActive = false)]
		int engineType;

		[KSPField (isPersistant = true, guiActive = false)]
		int engineAdv;

		//-=-=-=

		bool GUIOpen = false;

		//-=-=-=

		[KSPEvent (active = true, advancedTweakable = false, guiActive = false, guiActiveEditor = true, guiName = "Toggle Engine GUI")]
		public void ToggleGUI () {
			if (mainBox == null) {
				BuildGUI ();
			}
			GUIOpen = !GUIOpen;
			mainBox.SetActive (GUIOpen);
		}

		public void ChangeEngineType (int type) {
			if (GUIDone) {
				engineType = availableEngines[type];
				ResetEngineAdvDropdown ();
				UpdateEngine ();
			}
		}

		public void ChangeEngineAdv (int level) {
			if (GUIDone) {
				engineAdv = level;
				UpdateEngine ();
			}
		}

		public void UpdateEngine (bool updateText = true) {
			ActuallyUpdateEngine (updateText);

			PEngine p;
			foreach (Part i in this.part.symmetryCounterparts) {
				p = i.Modules.GetModule<PEngine> ();

				p.engineType = this.engineType;
				p.engineAdv = this.engineAdv;
				p.ActuallyUpdateEngine (true);
				if (p.GUIDone) {
					p.UpdateDropdownValues ();
				}
			}
		}

		public void ActuallyUpdateEngine (bool updateText = true) {
			this.part.Modules.GetModule<ModuleEngines> ().maxFuelFlow = engineBaseThrust[engineType * 3 + engineAdv] / engineBaseVISP[engineType * 3 + engineAdv] / 9.80665f;

			FloatCurve curve = new FloatCurve ();
			curve.Add (0.0f, engineBaseVISP[engineType * 3 + engineAdv]);
			curve.Add (1.0f, engineBaseAISP[engineType * 3 + engineAdv]);
			Cost = engineBaseCost[engineType * 3 + engineAdv];

			this.part.Modules.GetModule<ModuleEngines> ().atmosphereCurve = curve;

			if (updateText && VIspText != null) {
				UpdateText ();
			}
		}

		public void UpdateText () {
			VIspText.text = $"Vacuum ISP: {engineBaseVISP[engineType * 3 + engineAdv]}s";
			AIspText.text = $"Sea level ISP: {engineBaseAISP[engineType * 3 + engineAdv]}s";
			ThrustText.text = $"Vacuum thrust: {engineBaseThrust[engineType * 3 + engineAdv]}kN";
			MassText.text = $"Mass: {engineBaseMass[engineType * 3 + engineAdv]}t";
		}

		public void UpdateDropdownValues () {
			GUIDone = false;
			EngineTypeDropdown.value = availableEngines.FindIndex (a => availableEngines[a] == engineType);
			EngineAdvDropdown.value = engineAdv;

			EngineTypeDropdown.RefreshShownValue ();
			EngineAdvDropdown.RefreshShownValue ();
			GUIDone = true;
		}

		Canvas mainCanvas;
		GameObject mainBox;
		Dropdown EngineTypeDropdown;
		Dropdown EngineAdvDropdown;
		List<int> availableEngines = new List<int> ();
		Text VIspText;
		Text AIspText;
		Text ThrustText;
		Text MassText;
		bool GUIDone = false;

		public void BuildGUI () {
			GameObject canvas = new GameObject ("PEngineCanvas");
			mainCanvas = canvas.AddComponent<Canvas> ();
			mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.AddComponent<CanvasScaler> ();
			canvas.AddComponent<GraphicRaycaster> ();

			mainBox = new GameObject ("PEngineBox");
			mainBox.transform.SetParent (mainCanvas.transform);

			RectTransform mainBoxRect = mainBox.AddComponent<RectTransform> ();
			mainBoxRect.anchorMin = Vector2.zero;
			mainBoxRect.anchorMax = Vector2.zero;
			mainBoxRect.pivot = Vector2.zero;
			mainBoxRect.anchoredPosition = Vector2.zero;
			mainBoxRect.sizeDelta = Vector2.zero;

			GameObject mainPanel = UIUtils.CreateUIPanel (
				mainBox.transform,
				Vector2.zero,
				Vector2.zero,
				Vector2.zero,
				new Vector2 (100, 100),
				new Vector2 (320, 320),
				new Color (0.6f, 0.6f, 0.6f, 0.75f),
				""
			);//Background

			UIUtils.CreateUIPanel (
				mainPanel.transform,
				new Vector2 (0, 1),
				new Vector2 (0, 1),
				new Vector2 (0, 1),
				new Vector2 (0, 0),
				new Vector2 (280, 40),
				new Color (0.2f, 0.2f, 0.2f, 1.0f),
				"Engine configuration"
			).AddComponent<windowDragger> ().target = mainBox.GetComponent<RectTransform> ();//Dragging bar

			UIUtils.CreateUIButton (
				ToggleGUI,
				mainPanel.transform,
				Vector2.one,
				Vector2.one,
				Vector2.one,
				Vector2.zero,
				new Vector2 (40, 40),
				new Color (1.0f, 0.0f, 0.0f, 1.0f),
				"X"
			);//Close

			EngineTypeDropdown = UIUtils.CreateUIDropdown (
				mainPanel.transform,
				new Vector2 (0, 1),
				new Vector2 (0, 1),
				new Vector2 (0, 1),
				new Vector2 (10, -50),
				new Vector2 (200, 40),
				new Color (0.2f, 0.2f, 0.2f, 1.0f),
				engineName.ToList (),
				ChangeEngineType
			);//Engine cycle dropdown
			EngineTypeDropdown.options.Clear ();
			for (int i = 0; i < engineName.Length; ++i) {
				if (upgradeLevels[i] == 0) {
					continue;
				} else {
					availableEngines.Add (i);
					EngineTypeDropdown.options.Add (new Dropdown.OptionData (engineName[i]));
					if (engineType == i) {
						EngineTypeDropdown.value = availableEngines.Count;
					}
				}
			}
			EngineTypeDropdown.RefreshShownValue ();

			EngineAdvDropdown = UIUtils.CreateUIDropdown (
				mainPanel.transform,
				new Vector2 (0, 1),
				new Vector2 (0, 1),
				new Vector2 (0, 1),
				new Vector2 (220, -50),
				new Vector2 (90, 40),
				new Color (0.2f, 0.2f, 0.2f, 1.0f),
				new string[] { "Early", "Mid", "Modern" }.ToList (),
				ChangeEngineAdv
			);//Advancement level dropdown
			ResetEngineAdvDropdown (false);
			EngineAdvDropdown.value = engineAdv;
			EngineAdvDropdown.RefreshShownValue ();

			VIspText = UIUtils.CreateUIText (
				mainPanel.transform,
				new Vector2 (0.0f, 1.0f),
				new Vector2 (0.0f, 1.0f),
				new Vector2 (0.0f, 1.0f),
				new Vector2 (10.0f, -100.0f),
				new Vector2 (300.0f, 40.0f),
				16,
				"Placeholder VacISP"
			);//Vacuum ISP text

			AIspText = UIUtils.CreateUIText (
				mainPanel.transform,
				new Vector2 (0.0f, 1.0f),
				new Vector2 (0.0f, 1.0f),
				new Vector2 (0.0f, 1.0f),
				new Vector2 (10.0f, -150.0f),
				new Vector2 (300.0f, 40.0f),
				16,
				"Placeholder AtmISP"
			);//atm ISP text

			ThrustText = UIUtils.CreateUIText (
				mainPanel.transform,
				new Vector2 (0.0f, 1.0f),
				new Vector2 (0.0f, 1.0f),
				new Vector2 (0.0f, 1.0f),
				new Vector2 (10.0f, -200.0f),
				new Vector2 (300.0f, 40.0f),
				16,
				"Placeholder Thrust"
			);//thrust text

			MassText = UIUtils.CreateUIText (
				mainPanel.transform,
				new Vector2 (0.0f, 1.0f),
				new Vector2 (0.0f, 1.0f),
				new Vector2 (0.0f, 1.0f),
				new Vector2 (10.0f, -250.0f),
				new Vector2 (300.0f, 40.0f),
				16,
				"Placeholder Mass"
			);//mass text

			GUIDone = true;
			UpdateText ();
			mainBox.SetActive (false);
		}

		void ResetEngineAdvDropdown (bool changedType = true) {
			EngineAdvDropdown.options.Clear ();
			if (upgradeLevels[engineType] >= 1) EngineAdvDropdown.options.Add (new Dropdown.OptionData ("Early"));
			if (upgradeLevels[engineType] >= 2) EngineAdvDropdown.options.Add (new Dropdown.OptionData ("Mid"));
			if (upgradeLevels[engineType] >= 3) EngineAdvDropdown.options.Add (new Dropdown.OptionData ("Modern"));

			if (changedType) {
				EngineAdvDropdown.value = Math.Min (EngineAdvDropdown.options.Count, EngineAdvDropdown.value);
				EngineAdvDropdown.RefreshShownValue ();
				ChangeEngineAdv (EngineAdvDropdown.value);
			}
		}

		public void Start () {
			UpdateEngine (false);
		}

		public void OnDisable () {
			if (mainBox != null) {
				Destroy (mainBox);
			}
		}

		/*
		void OnGUI () {
			if (GUIOpen) {
				GUI.Box (new Rect (300, 300, 900, 600), "Procedural Engine GUI");

				if (GUI.Button (new Rect (1160, 300, 40, 40), "X")) {
					GUIOpen = false;
				}

				if (GUI.Button (new Rect (320, 320, 140, 40), "Add ISP")) {
					AddISP ();
				}

				if (GUI.Button (new Rect (320, 380, 140, 40), "Remove ISP")) {
					RemoveISP ();
				}
			}
		}
		*/

		[KSPAction ("Akcja")]
		public void myAction (KSPActionParam param) {
			Debug.Log ("AAAAAAAAA");
		}

		public ModifierChangeWhen GetModuleCostChangeWhen () {
			return ModifierChangeWhen.FIXED;
		}

		public float GetModuleCost (float stockCost, ModifierStagingSituation wat) {
			return Cost * new float[] {1.0f, 0.75f, 0.5f}[upgradeLevels[engineType] - 1];
		}

		public ModifierChangeWhen GetModuleMassChangeWhen () {
			return ModifierChangeWhen.FIXED;
		}

		public float GetModuleMass (float stockMass, ModifierStagingSituation wat) {
			return engineBaseMass[engineType * 3 + engineAdv];
	}

	}
}

class windowDragger : EventTrigger {

	public RectTransform target;

	public override void OnDrag (PointerEventData data) {
		target.anchoredPosition += data.delta;
	}
}