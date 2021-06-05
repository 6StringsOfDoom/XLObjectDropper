using GameManagement;
using ReplayEditor;
using System.Collections.Generic;
using System.Linq;
using SkaterXL.Data;
using UnityEngine;

namespace XLObjectDropper.Utilities
{
	public class RigidbodyObjectTracker : MonoBehaviour
	{
		private List<RigidbodyReplayInfo> replayInfos;
		private Rigidbody rigidbody;
		private float nextRecordTime;
		private AnimationClip clip;
		private Animation animation;
		private TransformInfo lastTransformInfo;

		private void Awake()
		{
			replayInfos = new List<RigidbodyReplayInfo>();
			rigidbody = GetComponent<Rigidbody>();

			clip = new AnimationClip();
			clip.legacy = true;
			clip.name = gameObject.name;

			animation = gameObject.GetComponentInChildren<Animation>(true);

			if (animation == null)
			{
				animation = gameObject.AddComponent<Animation>();
			}
		}

		private void PlaybackController_OnTimeChanged(float time, float timeScale)
		{
			//replayInfos.RemoveAll(x => x.time < ReplayEditorController.Instance.playbackController.ClipStartTime);

			var prevFrameIndex = ReplayEditorController.Instance.playbackController.prevFrameIndex;
			var previousFrameTime = ReplayEditorController.Instance.playbackController.ClipFrames[prevFrameIndex].time;

			if (previousFrameTime < replayInfos.FirstOrDefault().time) return;

			//var previousFrame = replayInfos.OrderByDescending(x => x.time).FirstOrDefault(x => x.time <= previousFrameTime);
			//var currentFrame = replayInfos[replayInfos.IndexOf(previousFrame) + 1];
			//var nextFrame = replayInfos[replayInfos.IndexOf(previousFrame) + 2];

			
		}

		private void Update()
		{
			if (GameStateMachine.Instance.CurrentState.GetType() == typeof(PlayState))
			{
				if (rigidbody.isKinematic)
				{
					if (animation != null && animation.isPlaying)
					{
						animation.Stop();
					}

					transform.localPosition = lastTransformInfo.position;
					transform.localRotation = lastTransformInfo.rotation;
					rigidbody.isKinematic = false;
				}

				RecordFrame();
			}

			if (GameStateMachine.Instance.CurrentState.GetType() == typeof(ReplayState))
			{
				if (!rigidbody.isKinematic)
				{
					lastTransformInfo = new TransformInfo(transform);
					rigidbody.isKinematic = true;
				}

				var replayInfosToAdd = replayInfos.Where(x => !x.addedToAnimation);
				if (replayInfosToAdd.Any())
				{
					var posXCurve = new AnimationCurve();
					var posYCurve = new AnimationCurve();
					var posZCurve = new AnimationCurve();

					var rotXCurve = new AnimationCurve();
					var rotYCurve = new AnimationCurve();
					var rotZCurve = new AnimationCurve();
					var rotWCurve = new AnimationCurve();

					foreach (var replayInfo in replayInfosToAdd)
					{
						posXCurve.AddKey(replayInfo.time, replayInfo.transformInfo.position.x);
						posYCurve.AddKey(replayInfo.time, replayInfo.transformInfo.position.y);
						posZCurve.AddKey(replayInfo.time, replayInfo.transformInfo.position.z);

						rotXCurve.AddKey(replayInfo.time, replayInfo.transformInfo.rotation.x);
						rotYCurve.AddKey(replayInfo.time, replayInfo.transformInfo.rotation.y);
						rotZCurve.AddKey(replayInfo.time, replayInfo.transformInfo.rotation.z);
						rotWCurve.AddKey(replayInfo.time, replayInfo.transformInfo.rotation.w);

						replayInfo.addedToAnimation = true;
					}

					clip.SetCurve("", typeof(Transform), "localPosition.x", posXCurve);
					clip.SetCurve("", typeof(Transform), "localPosition.y", posYCurve);
					clip.SetCurve("", typeof(Transform), "localPosition.z", posZCurve);

					clip.SetCurve("", typeof(Transform), "localRotation.x", rotXCurve);
					clip.SetCurve("", typeof(Transform), "localRotation.y", rotYCurve);
					clip.SetCurve("", typeof(Transform), "localRotation.z", rotZCurve);
					clip.SetCurve("", typeof(Transform), "localRotation.w", rotWCurve);
				}

				animation.AddClip(clip, clip.name);
				animation.animatePhysics = true;

				var state = animation[clip.name];

				if (!animation.isPlaying && ReplayEditorController.Instance.playbackController.TimeScale != 0.0)
					animation.Play(clip.name);

				state.time = ReplayEditorController.Instance.playbackController.CurrentTime;
				state.speed = ReplayEditorController.Instance.playbackController.TimeScale;
			}
		}

		private void RecordFrame()
		{
			if (nextRecordTime > PlayTime.time) return;

			if (nextRecordTime < PlayTime.time - 1f)
			{
				nextRecordTime = PlayTime.time + 1f / 30f;
			}
			else
			{
				nextRecordTime += 1f / 30f;
			}

			replayInfos.Add(new RigidbodyReplayInfo(transform, PlayTime.time));
		}
	}
}
