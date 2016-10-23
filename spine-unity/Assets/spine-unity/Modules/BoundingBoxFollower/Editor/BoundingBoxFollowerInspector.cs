/******************************************************************************
 * Spine Runtimes Software License v2.5
 *
 * Copyright (c) 2013-2016, Esoteric Software
 * All rights reserved.
 *
 * You are granted a perpetual, non-exclusive, non-sublicensable, and
 * non-transferable license to use, install, execute, and perform the Spine
 * Runtimes software and derivative works solely for personal or internal
 * use. Without the written permission of Esoteric Software (see Section 2 of
 * the Spine Software License Agreement), you may not (a) modify, translate,
 * adapt, or develop new applications using the Spine Runtimes or otherwise
 * create derivative works or improvements of the Spine Runtimes or (b) remove,
 * delete, alter, or obscure any trademarks or any copyright, trademark, patent,
 * or other intellectual property or proprietary rights notices on or in the
 * Software, including any copy thereof. Redistributions in binary or source
 * form must include this license and terms.
 *
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE "AS IS" AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
 * EVENT SHALL ESOTERIC SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, BUSINESS INTERRUPTION, OR LOSS OF
 * USE, DATA, OR PROFITS) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
 * IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using UnityEngine;
using UnityEditor;

namespace Spine.Unity.Editor {

	[CustomEditor(typeof(BoundingBoxFollower))]
	public class BoundingBoxFollowerInspector : UnityEditor.Editor {
		SerializedProperty skeletonRenderer, slotName, isTrigger, clearStateOnDisable;
		BoundingBoxFollower follower;
		bool rebuildRequired = false;
		bool addBoneFollower = false;
		bool debugIsExpanded;

		void OnEnable () {
			skeletonRenderer = serializedObject.FindProperty("skeletonRenderer");
			slotName = serializedObject.FindProperty("slotName");
			isTrigger = serializedObject.FindProperty("isTrigger");
			clearStateOnDisable = serializedObject.FindProperty("clearStateOnDisable");
			follower = (BoundingBoxFollower)target;
		}

		public override void OnInspectorGUI () {
			bool isInspectingPrefab = (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab);
			bool repaintEvent = UnityEngine.Event.current.type == EventType.Repaint;

			if (rebuildRequired) {
				follower.HandleRebuild(null);
				rebuildRequired = false;
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(skeletonRenderer);
			EditorGUILayout.PropertyField(slotName, new GUIContent("Slot"));
			EditorGUILayout.PropertyField(isTrigger);
			EditorGUILayout.PropertyField(clearStateOnDisable, new GUIContent(clearStateOnDisable.displayName, "Enable this if you are pooling your Spine GameObject"));

			if (EditorGUI.EndChangeCheck()) {
				serializedObject.ApplyModifiedProperties();
				if (!isInspectingPrefab)
					rebuildRequired = true;
			}

			if (isInspectingPrefab) {
				follower.colliderTable.Clear();
				follower.attachmentNameTable.Clear();
				EditorGUILayout.HelpBox("BoundingBoxAttachments cannot be previewed in prefabs.", MessageType.Info);

				// How do you prevent components from being saved into the prefab? No such HideFlag. DontSaveInEditor | DontSaveInBuild does not work. DestroyImmediate does not work.
				var collider = follower.GetComponent<PolygonCollider2D>();
				if (collider != null) Debug.LogWarning("Found BoundingBoxFollower collider components in prefab. These are disposed and regenerated at runtime.");

			} else {
				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
					EditorGUI.indentLevel++;
					debugIsExpanded = EditorGUILayout.Foldout(debugIsExpanded, "Debug Colliders");
					if (debugIsExpanded) {
						EditorGUI.indentLevel++;
						EditorGUILayout.LabelField(string.Format("Attachment Names ({0} PolygonCollider2D)", follower.colliderTable.Count));
						EditorGUI.BeginChangeCheck();
						foreach (var kp in follower.attachmentNameTable) {
							string attachmentName = kp.Value;
							var collider = follower.colliderTable[kp.Key];
							bool isPlaceholder = attachmentName != kp.Key.Name;
							collider.enabled = EditorGUILayout.ToggleLeft(new GUIContent(!isPlaceholder ? attachmentName : attachmentName + " [" + kp.Key.Name + "]", isPlaceholder ? SpineEditorUtilities.Icons.skinPlaceholder : SpineEditorUtilities.Icons.boundingBox), collider.enabled);
						}
						if (EditorGUI.EndChangeCheck())
							SceneView.RepaintAll();
						EditorGUI.indentLevel--;
					}
					EditorGUI.indentLevel--;
				}

			}

			bool hasBoneFollower = follower.GetComponent<BoneFollower>() != null;
			using (new EditorGUI.DisabledGroupScope(hasBoneFollower || follower.Slot == null)) {
				using (new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.Space();
					if (GUILayout.Button(new GUIContent("Add Bone Follower", SpineEditorUtilities.Icons.bone), GUILayout.MaxWidth(300f), GUILayout.Height(40f)))
						addBoneFollower = true;
					EditorGUILayout.Space();
				}
				EditorGUILayout.Space();
			}

			if (addBoneFollower && repaintEvent) {
				var boneFollower = follower.gameObject.AddComponent<BoneFollower>();
				boneFollower.boneName = follower.Slot.Data.BoneData.Name;
				addBoneFollower = false;
			}
		}

	}

}
