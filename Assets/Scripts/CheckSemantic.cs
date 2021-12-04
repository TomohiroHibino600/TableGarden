using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Niantic.ARDK.AR;
using Niantic.ARDK.Utilities;

using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;

using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Semantics;

using Niantic.ARDK.Extensions;

namespace TableGarden
{
    public class CheckSemantic : MonoBehaviour
    {
        ISemanticBuffer currentBuffer;
        [SerializeField] private ARSemanticSegmentationManager semanticManager;
        [SerializeField] private Camera ARCamera;

        void Start()
        {
            //add a callback for catching the updated semantic buffer
            semanticManager.SemanticBufferUpdated += OnSemanticsBufferUpdated;
        }

        private void OnSemanticsBufferUpdated(ContextAwarenessStreamUpdatedArgs<ISemanticBuffer> args)
        {
            //get the current buffer
            currentBuffer = args.Sender.AwarenessBuffer;
        }

        // Update is called once per frame
        void Update()
        {
            if (PlatformAgnosticInput.touchCount <= 0) { return; }

            var touch = PlatformAgnosticInput.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                int x = (int)touch.position.x;
                int y = (int)touch.position.y;
                DebugLogSemanticsAt(x, y);
            }
        }

        //examples of all the functions you can use to interogate the procider/biffers
        void DebugLogSemanticsAt(int x, int y)
        {
            string[] channelsNamesInPixel = semanticManager.SemanticBufferProcessor.GetChannelNamesAt(x, y);
            foreach (var i in channelsNamesInPixel)
            {
                Debug.Log($"{i}");
            }
        }
    }
}
