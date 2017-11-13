using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ConsoleApp1 {
	public static class UIUtils {
		public static GameObject CreateUIPanel (Transform parent, Vector2 minAnchor, Vector2 maxAnchor, Vector2 pivot, Vector2 position, Vector2 size, Color color, string text = "") {
			GameObject toReturn = new GameObject ("PEngineGUI");
			toReturn.transform.SetParent (parent);

			RectTransform rectT = toReturn.AddComponent<RectTransform> ();
			rectT.anchorMin = minAnchor;
			rectT.anchorMax = maxAnchor;
			rectT.pivot = pivot;
			rectT.anchoredPosition = position;
			rectT.sizeDelta = size;
			toReturn.AddComponent<Image> ().color = color;

			CreateUIText (
				toReturn.transform,
				Vector2.zero,
				Vector2.one,
				new Vector2 (0.5f, 0.5f),
				Vector2.zero,
				Vector2.zero,
				16,
				text
			);

			return toReturn;
		}

		public static Dropdown CreateUIDropdown (Transform parent, Vector2 minAnchor, Vector2 maxAnchor, Vector2 pivot, Vector2 position, Vector2 size, Color color, List<string> options, UnityEngine.Events.UnityAction<int> action) {
			GameObject toReturn = CreateUIPanel (parent, minAnchor, maxAnchor, pivot, position, size, color);
			Dropdown dropdown = toReturn.AddComponent<Dropdown> ();

			GameObject template = CreateUIPanel (toReturn.transform, new Vector2 (0, 1), new Vector2 (0, 1), new Vector2 (0, 1), new Vector2 (0, -size.y), size, color);
			UnityEngine.Object.Destroy (template.GetComponentInChildren<Text> ().gameObject);
			template.SetActive (false);
			Canvas temp = template.AddComponent<Canvas> ();
			temp.overrideSorting = true;
			temp.sortingOrder = 30000;
			template.AddComponent<GraphicRaycaster> ();
			template.AddComponent<CanvasGroup> ();

			GameObject item = CreateUIPanel (template.transform, new Vector2 (0, 1), new Vector2 (0, 1), new Vector2 (0, 1), Vector2.zero, size, color);
			item.AddComponent<Toggle> ();

			dropdown.captionText = toReturn.GetComponentInChildren<Text> ();
			dropdown.template = template.GetComponent<RectTransform> ();
			dropdown.itemText = item.GetComponentInChildren<Text> ();

			dropdown.options.Clear ();
			for (int i = 0; i < options.Count; ++i) {
				dropdown.options.Add (new Dropdown.OptionData (options[i]));
			}
			dropdown.value = 0;
			dropdown.RefreshShownValue ();
			dropdown.onValueChanged.AddListener (action);

			return dropdown;
		}

		public static Text CreateUIText (Transform parent, Vector2 minAnchor, Vector2 maxAnchor, Vector2 pivot, Vector2 position, Vector2 size, int fontSize, string text = "") {
			GameObject toReturn = new GameObject ("PEngineGUIText");
			toReturn.transform.SetParent (parent);

			RectTransform rectT = toReturn.AddComponent<RectTransform> ();
			rectT.anchorMin = minAnchor;
			rectT.anchorMax = maxAnchor;
			rectT.pivot = pivot;
			rectT.anchoredPosition = position;
			rectT.sizeDelta = size;

			Text textT = toReturn.AddComponent<Text> ();
			textT.text = text;
			textT.fontSize = fontSize;
			textT.font = Resources.GetBuiltinResource<Font> ("Arial.ttf");
			textT.alignment = TextAnchor.MiddleCenter;

			return textT;
		}

		public static GameObject CreateUIButton (UnityEngine.Events.UnityAction action, Transform parent, Vector2 minAnchor, Vector2 maxAnchor, Vector2 pivot, Vector2 position, Vector2 size, Color color, string text = "") {
			GameObject toReturn = CreateUIPanel (parent, minAnchor, maxAnchor, pivot, position, size, color, text);

			toReturn.AddComponent<Button> ().onClick.AddListener (action);

			return toReturn;
		}
	}
}
