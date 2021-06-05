using SkaterXL.Data;
using UnityEngine;

namespace XLObjectDropper.Utilities
{
	public class RigidbodyReplayInfo
	{
		public TransformInfo transformInfo;
		public float time;

		public bool addedToAnimation;

		public RigidbodyReplayInfo(Transform transform, float time)
		{
			transformInfo = new TransformInfo(transform);
			this.time = time;
			this.addedToAnimation = false;
		}
	}
}
