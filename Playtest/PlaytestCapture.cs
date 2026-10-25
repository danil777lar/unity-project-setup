using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Larje.ProjectSetup.Editor.Playtest
{
    public sealed class PlaytestCapture
    {
        private readonly int _width;
        private readonly int _height;
        private readonly string _folder;

        public PlaytestCapture(string folder, int width, int height)
        {
            _folder = folder;
            _width = width;
            _height = height;
            Directory.CreateDirectory(folder);
        }

        public string Capture(string name)
        {
            RenderTexture texture = new RenderTexture(_width, _height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            texture.Create();
            List<OverlayState> overlays = new List<OverlayState>();
            Camera uiCamera = null;
            Camera top = null;
            int topCullingMask = 0;
            try
            {
                List<Camera> cameras = Camera.allCameras.Where(x => x.enabled && x.targetTexture == null).OrderBy(x => x.depth).ToList();
                if (cameras.Count == 0)
                {
                    uiCamera = CreateUiCamera();
                    cameras.Add(uiCamera);
                }

                top = cameras[cameras.Count - 1];
                topCullingMask = top.cullingMask;
                foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.isActiveAndEnabled)
                    {
                        overlays.Add(new OverlayState(canvas, canvas.planeDistance));
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = top;
                        canvas.planeDistance = top.nearClipPlane + 0.05f;
                        top.cullingMask |= GetLayerMask(canvas);
                    }
                }

                Canvas.ForceUpdateCanvases();
                for (int i = 0; i < cameras.Count; i++)
                {
                    RenderInto(cameras[i], texture, i == 0);
                }

                return Save(texture, name);
            }
            finally
            {
                foreach (OverlayState overlay in overlays)
                {
                    overlay.Restore();
                }

                if (top != null)
                {
                    top.cullingMask = topCullingMask;
                }

                if (uiCamera != null)
                {
                    Object.DestroyImmediate(uiCamera.gameObject);
                }

                texture.Release();
                Object.DestroyImmediate(texture);
            }
        }

        private Camera CreateUiCamera()
        {
            GameObject cameraObject = new GameObject("PlaytestUiCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.cullingMask = 0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 10f;
            camera.enabled = false;
            return camera;
        }

        private int GetLayerMask(Canvas canvas)
        {
            int mask = 0;
            foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
            {
                mask |= 1 << child.gameObject.layer;
            }

            return mask;
        }

        private void RenderInto(Camera camera, RenderTexture texture, bool first)
        {
            RenderTexture previousTarget = camera.targetTexture;
            CameraClearFlags previousFlags = camera.clearFlags;
            camera.targetTexture = texture;
            if (!first && camera.clearFlags != CameraClearFlags.Nothing)
            {
                camera.clearFlags = CameraClearFlags.Depth;
            }

            camera.Render();
            camera.targetTexture = previousTarget;
            camera.clearFlags = previousFlags;
        }

        private string Save(RenderTexture texture, string name)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            Texture2D readback = new Texture2D(_width, _height, TextureFormat.RGB24, false);
            readback.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            readback.Apply();
            RenderTexture.active = previous;
            string path = Path.Combine(_folder, name + ".png");
            File.WriteAllBytes(path, readback.EncodeToPNG());
            Object.DestroyImmediate(readback);
            return path;
        }

        private readonly struct OverlayState
        {
            private readonly Canvas _canvas;
            private readonly float _planeDistance;

            public OverlayState(Canvas canvas, float planeDistance)
            {
                _canvas = canvas;
                _planeDistance = planeDistance;
            }

            public void Restore()
            {
                if (_canvas != null)
                {
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _canvas.worldCamera = null;
                    _canvas.planeDistance = _planeDistance;
                }
            }
        }
    }
}
