﻿using System.Collections.Generic;
using UnityEngine;

namespace XLObjectDropper.EventStack.Events
{
	public abstract class ObjectDropperEvent
	{
		public GameObject GameObject;
		public List<GameObject> GameObjects;

		public abstract void Undo();
		public abstract void Redo();

		/// <summary>
		/// Override this to do any kind of validation before pushing to the stack.
		/// </summary>
		public virtual void AddToUndoStack()
		{
			EventStack.Instance.UndoQueue.Push(this);
		}

		public ObjectDropperEvent(GameObject gameObject)
		{
			GameObject = gameObject;
		}

		public ObjectDropperEvent(List<GameObject> gameObjects)
		{
			GameObjects = gameObjects;
		}
	}
}
