

/*****************************************************************************
 * Skeleton Utility created by Mitch Thompson
 * Full irrevocable rights and permissions granted to Esoteric Software
*****************************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Spine;

namespace Spine.Unity {
	/// <summary>Sets a GameObject's transform to match a bone on a Spine skeleton.</summary>
	[ExecuteInEditMode]
	[AddComponentMenu("Spine/SkeletonUtilityBone")]
	public class SkeletonUtilityBone : MonoBehaviour {

		public enum Mode {
			Follow,
			Override
		}

		[System.NonSerialized]
		public bool valid;
		[System.NonSerialized]
		public SkeletonUtility skeletonUtility;
		[System.NonSerialized]
		public Bone bone;
		public Mode mode;
		public bool zPosition = true;
		public bool position;
		public bool rotation;
		public bool scale;
		// MITCH : remove flipX
		// Kept these fields public to retain serialization. Probably remove eventually?
		public bool flip;
		public bool flipX;
		[Range(0f, 1f)]
		public float overrideAlpha = 1;

		/// <summary>If a bone isn't set, boneName is used to find the bone.</summary>
		public String boneName;
		public Transform parentReference;

		[System.NonSerialized]
		public bool transformLerpComplete;

		protected Transform cachedTransform;
		protected Transform skeletonTransform;

		// MITCH : nonuniform scale
		//	private bool nonUniformScaleWarning;
		//	public bool NonUniformScaleWarning {
		//		get { return nonUniformScaleWarning; }
		//	}

		private bool incompatibleTransformMode;
		public bool IncompatibleTransformMode {
			get { return incompatibleTransformMode; }
		}

		public void Reset () {
			bone = null;
			cachedTransform = transform;
			valid = skeletonUtility != null && skeletonUtility.skeletonRenderer != null && skeletonUtility.skeletonRenderer.valid;
			if (!valid)
				return;
			skeletonTransform = skeletonUtility.transform;
			skeletonUtility.OnReset -= HandleOnReset;
			skeletonUtility.OnReset += HandleOnReset;
			DoUpdate();
		}

		void OnEnable () {
			skeletonUtility = SkeletonUtility.GetInParent<SkeletonUtility>(transform);

			if (skeletonUtility == null)
				return;

			skeletonUtility.RegisterBone(this);
			skeletonUtility.OnReset += HandleOnReset;
		}

		void HandleOnReset () {
			Reset();
		}

		void OnDisable () {
			if (skeletonUtility != null) {
				skeletonUtility.OnReset -= HandleOnReset;
				skeletonUtility.UnregisterBone(this);
			}
		}

		public void DoUpdate () {
			if (!valid) {
				Reset();
				return;
			}

			Spine.Skeleton skeleton = skeletonUtility.skeletonRenderer.skeleton;

			if (bone == null) {
				if (string.IsNullOrEmpty(boneName))
					return;

				bone = skeleton.FindBone(boneName);

				if (bone == null) {
					Debug.LogError("Bone not found: " + boneName, this);
					return;
				}
			}

			float skeletonFlipRotation = (skeleton.flipX ^ skeleton.flipY) ? -1f : 1f;

			// MITCH : remove flipX
			//		float flipCompensation = 0;
			//		if (flip && (flipX || (flipX != bone.flipX)) && bone.parent != null) {
			//			flipCompensation = bone.parent.WorldRotation * -2;
			//		}

			if (mode == Mode.Follow) {
				// MITCH : remove flipX
				//			if (flip)
				//				flipX = bone.flipX;

				if (position)
					cachedTransform.localPosition = new Vector3(bone.x, bone.y, 0);

				if (rotation) {
					if (!bone.data.transformMode.InheritsRotation()) {
						// MITCH : remove flipX
						//if (bone.FlipX) {
						//	cachedTransform.localRotation = Quaternion.Euler(0, 180, bone.rotationIK - flipCompensation);
						//} else {
						cachedTransform.localRotation = Quaternion.Euler(0, 0, bone.AppliedRotation);
						//}
					} else {
						Vector3 euler = skeletonTransform.rotation.eulerAngles;
						cachedTransform.rotation = Quaternion.Euler(euler.x, euler.y, euler.z + (bone.WorldRotationX * skeletonFlipRotation));
					}
				}

				if (scale) {
					cachedTransform.localScale = new Vector3(bone.scaleX, bone.scaleY, 1f);//, bone.WorldSignX);
					incompatibleTransformMode = BoneTransformModeIncompatible(bone);
				}

			} else if (mode == Mode.Override) {
				if (transformLerpComplete)
					return;

				if (parentReference == null) {
					if (position) {
						bone.x = Mathf.Lerp(bone.x, cachedTransform.localPosition.x, overrideAlpha);
						bone.y = Mathf.Lerp(bone.y, cachedTransform.localPosition.y, overrideAlpha);
					}

					if (rotation) {
						float angle = Mathf.LerpAngle(bone.Rotation, cachedTransform.localRotation.eulerAngles.z, overrideAlpha);

						// MITCH : remove flipX
						//					float angle = Mathf.LerpAngle(bone.Rotation, cachedTransform.localRotation.eulerAngles.z, overrideAlpha) + flipCompensation;
						//					if (flip) {
						//                        
						//						if ((!flipX && bone.flipX)) {
						//							angle -= flipCompensation;
						//						}
						//
						//						//TODO fix this...
						//						if (angle >= 360)
						//							angle -= 360;
						//						else if (angle <= -360)
						//							angle += 360;
						//					}
						bone.Rotation = angle;
						bone.AppliedRotation = angle;
					}

					if (scale) {
						bone.scaleX = Mathf.Lerp(bone.scaleX, cachedTransform.localScale.x, overrideAlpha);
						bone.scaleY = Mathf.Lerp(bone.scaleY, cachedTransform.localScale.y, overrideAlpha);
						// MITCH : nonuniform scale
						//nonUniformScaleWarning = (bone.scaleX != bone.scaleY);
					}

					// MITCH : remove flipX
					//if (flip)
					//	bone.flipX = flipX;
				} else {
					if (transformLerpComplete)
						return;

					if (position) {
						Vector3 pos = parentReference.InverseTransformPoint(cachedTransform.position);
						bone.x = Mathf.Lerp(bone.x, pos.x, overrideAlpha);
						bone.y = Mathf.Lerp(bone.y, pos.y, overrideAlpha);
					}

					// MITCH
					if (rotation) {
						float angle = Mathf.LerpAngle(bone.Rotation, Quaternion.LookRotation(flipX ? Vector3.forward * -1 : Vector3.forward, parentReference.InverseTransformDirection(cachedTransform.up)).eulerAngles.z, overrideAlpha);

						// MITCH : remove flipX
						//					float angle = Mathf.LerpAngle(bone.Rotation, Quaternion.LookRotation(flipX ? Vector3.forward * -1 : Vector3.forward, parentReference.InverseTransformDirection(cachedTransform.up)).eulerAngles.z, overrideAlpha) + flipCompensation;
						//					if (flip) {
						//                        
						//						if ((!flipX && bone.flipX)) {
						//							angle -= flipCompensation;
						//						}
						//
						//						//TODO fix this...
						//						if (angle >= 360)
						//							angle -= 360;
						//						else if (angle <= -360)
						//							angle += 360;
						//					}
						bone.Rotation = angle;
						bone.AppliedRotation = angle;
					}

					if (scale) {
						bone.scaleX = Mathf.Lerp(bone.scaleX, cachedTransform.localScale.x, overrideAlpha);
						bone.scaleY = Mathf.Lerp(bone.scaleY, cachedTransform.localScale.y, overrideAlpha);
					}

					incompatibleTransformMode = BoneTransformModeIncompatible(bone);

					// MITCH : remove flipX
					//if (flip)
					//	bone.flipX = flipX;
				}

				transformLerpComplete = true;
			}
		}

		// MITCH : remove flipX
		//	public void FlipX (bool state) {
		//		if (state != flipX) {
		//			flipX = state;
		//			if (flipX && Mathf.Abs(transform.localRotation.eulerAngles.y) > 90) {
		//				skeletonUtility.skeletonAnimation.LateUpdate();
		//				return;
		//			} else if (!flipX && Mathf.Abs(transform.localRotation.eulerAngles.y) < 90) {
		//				skeletonUtility.skeletonAnimation.LateUpdate();
		//				return;
		//			}
		//		}
		//
		//        
		//		bone.FlipX = state;
		//		transform.RotateAround(transform.position, skeletonUtility.transform.up, 180);
		//		Vector3 euler = transform.localRotation.eulerAngles;
		//		euler.x = 0;
		//        
		//		euler.y = bone.FlipX ? 180 : 0;
		//        euler.y = 0;
		//		transform.localRotation = Quaternion.Euler(euler);
		//	}

		public static bool BoneTransformModeIncompatible (Bone bone) {
//			var transformMode = !bone.data.transformMode;
//			return (transformMode == TransformMode.NoScale || transformMode == TransformMode.NoScaleOrReflection || transformMode == TransformMode.OnlyTranslation);
			return !bone.data.transformMode.InheritsScale();
		}

		public void AddBoundingBox (string skinName, string slotName, string attachmentName) {
			SkeletonUtility.AddBoundingBox(bone.skeleton, skinName, slotName, attachmentName, transform);
		}

		#if UNITY_EDITOR
		void OnDrawGizmos () {
			if (IncompatibleTransformMode)
				Gizmos.DrawIcon(transform.position + new Vector3(0, 0.128f, 0), "icon-warning");		
		}
		#endif
	}
}
