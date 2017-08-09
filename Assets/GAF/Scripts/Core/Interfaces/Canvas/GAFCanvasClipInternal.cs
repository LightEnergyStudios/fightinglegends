using UnityEngine;
using System.Collections;
using GAFInternal.Objects;
using System;

namespace GAFInternal.Core
{
	public class GAFCanvasClipInternal<ObjectsManagerType> : IGAFCanvasClip where ObjectsManagerType : GAFCanvasObjectsManagerInternal
	{
		#region IGAFCanvasClip
		public float duration()
		{
			throw new NotImplementedException();
		}

		public GAFWrapMode getAnimationWrapMode()
		{
			throw new NotImplementedException();
		}

		public uint getCurrentFrameNumber()
		{
			throw new NotImplementedException();
		}

		public uint getCurrentSequenceIndex()
		{
			throw new NotImplementedException();
		}

		public uint getFramesCount()
		{
			throw new NotImplementedException();
		}

		public IGAFCanvasObject getObject(string _PartName)
		{
			throw new NotImplementedException();
		}

		public IGAFCanvasObject getObject(uint _ID)
		{
			throw new NotImplementedException();
		}

		public void gotoAndPlay(uint _FrameNumber)
		{
			throw new NotImplementedException();
		}

		public void gotoAndStop(uint _FrameNumber)
		{
			throw new NotImplementedException();
		}

		public bool isPlaying()
		{
			throw new NotImplementedException();
		}

		public string objectIDToPartName(uint _ID)
		{
			throw new NotImplementedException();
		}

		public uint partNameToObjectID(string _PartName)
		{
			throw new NotImplementedException();
		}

		public void pause()
		{
			throw new NotImplementedException();
		}

		public void play()
		{
			throw new NotImplementedException();
		}

		public void removeAllTriggers()
		{
			throw new NotImplementedException();
		}

		public void removeAllTriggers(uint _FrameNumber)
		{
			throw new NotImplementedException();
		}

		public void removeTrigger(int _ID)
		{
			throw new NotImplementedException();
		}

		public string sequenceIndexToName(uint _Index)
		{
			throw new NotImplementedException();
		}

		public uint sequenceNameToIndex(string _Name)
		{
			throw new NotImplementedException();
		}

		public void setAnimationWrapMode(GAFWrapMode _Mode)
		{
			throw new NotImplementedException();
		}

		public void setDefaultSequence(bool _PlayImmediately)
		{
			throw new NotImplementedException();
		}

		public void setSequence(string _SequenceName, bool _PlayImmediately)
		{
			throw new NotImplementedException();
		}

		public void stop()
		{
			throw new NotImplementedException();
		}
		#endregion // IGAFCanvasClip
	}
}