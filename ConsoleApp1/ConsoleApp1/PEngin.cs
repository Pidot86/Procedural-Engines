using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ConsoleApp1 {

	#region comments
	// Constructor
	// OnEnable
	// Start
	//
	// OnDisable

	// Constructor - mainPart
	// OnEnable    - mainPart
	// Start       - mainPart
	// Constructor - symmetryPart
	// OnEnable    - symmetryPart
	// Constructor - symmetryPart
	// OnEnable    - symmetryPart
	// Constructor - symmetryPart
	// OnEnable    - symmetryPart
	// Start       - symmetryPart
	// Start       - symmetryPart
	// Start       - symmetryPart
	#endregion

	class PEngin : PartModule {

		[KSPField (isPersistant = true, guiActive = false, guiActiveEditor = true, guiUnits = "m")]
		float diameter;

		/*
		float radius {
			set => diameter = value * 2;
			get => diameter / 2;
		}
		*/

		[KSPField (isPersistant = true, guiActive = false, guiActiveEditor = true, guiUnits = "m")]
		float height;

		GameObject model;
		GameObject bellModel;

		[KSPEvent (active = true, advancedTweakable = false, guiActive = false, guiActiveEditor = true, guiName = "Diameter +0.1m")]
		public void ANumber () {
			SymmetryUpdateModel (diameter + 0.1f, height);
		}

		[KSPEvent (active = true, advancedTweakable = false, guiActive = false, guiActiveEditor = true, guiName = "Diameter -0.1m")]
		public void BNumber () {
			SymmetryUpdateModel (diameter - 0.1f, height);
		}

		[KSPEvent (active = true, advancedTweakable = false, guiActive = false, guiActiveEditor = true, guiName = "Height +0.1m")]
		public void CNumber () {
			SymmetryUpdateModel (diameter, height + 0.1f);
		}

		[KSPEvent (active = true, advancedTweakable = false, guiActive = false, guiActiveEditor = true, guiName = "Height -0.1m")]
		public void DNumber () {
			SymmetryUpdateModel (diameter, height - 0.1f);
		}

		public PEngin () {
			
		}

		private void SymmetryUpdateModel (float r, float h) {
			this.UpdateModel (r / 2, h);

			foreach (Part i in part.symmetryCounterparts) {
				i.Modules.GetModule<PEngin> ().UpdateModel (r / 2, h);
			}
		}

		private void SymmetryMoveNode (Vector3 newTopPosition, Vector3 newBottomPosition) {
			this.MoveNode (newTopPosition, newBottomPosition);

			foreach (Part i in part.symmetryCounterparts) {
				i.Modules.GetModule<PEngin> ().MoveNode (newTopPosition, newBottomPosition);
			}
		}

		public void UpdateModel (float r, float h) {
			this.diameter = r * 2;
			this.height = h;
			model.transform.localScale = new Vector3 (r * 2.0f, h / 2.0f, r * 2.0f);

			bellModel.transform.position = model.transform.position - model.transform.up * (h / 2 + 0.25f);
			bellModel.transform.localScale = Vector3.one * 0.8f * 0.5f * 1.25f;

			MoveNode (new Vector3 (0, h / 2, 0), new Vector3 (0, -h / 2, 0));
		}

		public void MoveNode (Vector3 newTopPosition, Vector3 newBottomPosition) {

			Vector3 topDelta = newTopPosition - (part.attachNodes[0].position);
			Vector3 bottomDelta = newBottomPosition - (part.attachNodes[1].position);

			part.attachNodes[0].position = newTopPosition;
			part.attachNodes[1].position = newBottomPosition;

			this.part.transform.position -= (topDelta);

			if (this.part.attachNodes[1].attachedPart != null) {
				this.part.attachNodes[1].attachedPart.transform.position += (bottomDelta);
			}
		}

		private void BuildModel () {
			model = GameObject.CreatePrimitive (PrimitiveType.Cylinder);
			Destroy (model.GetComponent<CapsuleCollider> ());
			model.AddComponent<MeshCollider> ().convex = true;
			//model.AddComponent<BoxCollider> ();
			model.name = "PyGHKXwJRPiRw7ta";
			model.transform.SetParent (part.transform);
			model.transform.localPosition = Vector3.zero;
			model.transform.rotation = part.transform.rotation;
			
			//

			foreach (MeshFilter i in part.GetComponentsInChildren<MeshFilter> ()) {
				if (i.gameObject.name == "engine") {
					i.mesh.Clear ();
					Destroy (i.gameObject.GetComponent<MeshCollider> ());
				}

				if (i.gameObject.name == "obj_gimbal") {
					Destroy (i.gameObject.GetComponent<MeshCollider> ());
					i.mesh.Clear ();
					i.mesh.vertices = EngineBellMesh.vertsList ().ToArray ();
					i.mesh.triangles = EngineBellMesh.tris;
					i.mesh.uv = new Vector2[] { };
					bellModel = i.gameObject;
					bellModel.transform.rotation = part.transform.rotation;
					bellModel.transform.Rotate (-90.0f, 0.0f, 0.0f);
				}
			}
		}

		private bool FindModel () {
			model = part.gameObject.GetChild ("PyGHKXwJRPiRw7ta");

			foreach (MeshFilter i in part.GetComponentsInChildren<MeshFilter> ()) {
				if (i.gameObject.name == "obj_gimbal") {
					bellModel = i.gameObject;
				}
			}

			return model != null && bellModel != null;
		}

		public void Start () {
			if (!FindModel ()) {
				BuildModel ();
			}

			if (diameter <= 0) {
				diameter = 1f;
			}

			if (height <= 0) {
				height = 0.5f;
			}

			UpdateModel (diameter / 2, height);
		}

		public void OnEnable () {
			
		}

		public void OnDisable () {
			
		}
	}
}
