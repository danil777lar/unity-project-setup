using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Larje.ProjectSetup.Editor.Playtest
{
    public sealed class PlaytestSession : IDisposable
    {
        public const string OUTPUT_FOLDER = "Logs/Playtest";

        private int _frames;
        private float _frameTime;
        private float _worstFrame;
        private readonly string _name;
        private readonly PlaytestCapture _capture;
        private readonly PlaytestInput _input = new PlaytestInput();
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _steps = new List<string>();
        private readonly List<string> _screenshots = new List<string>();

        public PlaytestInput Input => _input;
        public IReadOnlyList<string> Errors => _errors;

        public PlaytestSession(string name, int width = 1080, int height = 1920)
        {
            _name = name;
            string folder = Path.Combine(OUTPUT_FOLDER, name);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            _capture = new PlaytestCapture(folder, width, height);
            Application.logMessageReceived += OnLog;
        }

        public IEnumerator LoadFirstScene()
        {
            Step($"Load scene {SceneUtility.GetScenePathByBuildIndex(0)}");
            SceneManager.LoadScene(0);
            yield return null;
        }

        public IEnumerator WaitForScene(string sceneName, float timeout = 20f)
        {
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == sceneName, timeout, $"scene {sceneName}");
        }

        public IEnumerator WaitSeconds(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                Sample();
                yield return null;
            }
        }

        public IEnumerator WaitUntil(Func<bool> condition, float timeout, string description)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < end)
            {
                Sample();
                yield return null;
            }

            Step(condition() ? $"Reached {description}" : $"Timed out waiting for {description}");
        }

        public string Capture(string label)
        {
            string path = _capture.Capture($"{_screenshots.Count:D2}-{label}");
            _screenshots.Add(path);
            Step($"Screenshot {path}");
            return path;
        }

        public List<string> DescribeButtons()
        {
            List<string> result = new List<string>();
            foreach (Button button in FindButtons())
            {
                result.Add($"{button.name} \"{GetLabel(button)}\" interactable={button.IsInteractable()}");
            }

            Step("Buttons: " + (result.Count == 0 ? "none" : string.Join("; ", result)));
            return result;
        }

        public bool Click(string nameOrLabel)
        {
            Button button = FindButtons().FirstOrDefault(x => x.IsInteractable() && Matches(x, nameOrLabel));
            if (button == null)
            {
                Step($"Button '{nameOrLabel}' not found");
                return false;
            }

            PointerEventData data = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
            Step($"Clicked '{button.name}'");
            return true;
        }

        public void Step(string message)
        {
            _steps.Add($"[{Time.realtimeSinceStartup:F1}s] {message}");
        }

        public string WriteReport()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Playtest: {_name}");
            builder.AppendLine($"Result: {(_errors.Count == 0 ? "OK" : $"{_errors.Count} errors")}");
            if (_frames > 0)
            {
                builder.AppendLine($"Average frame: {_frameTime / _frames * 1000f:F1} ms, worst: {_worstFrame * 1000f:F1} ms, frames: {_frames}");
            }

            builder.AppendLine();
            builder.AppendLine("Steps:");
            _steps.ForEach(x => builder.AppendLine("  " + x));
            builder.AppendLine();
            builder.AppendLine("Errors:");
            _errors.ForEach(x => builder.AppendLine("  " + x));
            string path = Path.Combine(OUTPUT_FOLDER, _name, "report.txt");
            File.WriteAllText(path, builder.ToString());
            return path;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLog;
            _input.Dispose();
        }

        private IEnumerable<Button> FindButtons()
        {
            return UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(x => x.isActiveAndEnabled);
        }

        private bool Matches(Button button, string nameOrLabel)
        {
            return button.name.IndexOf(nameOrLabel, StringComparison.OrdinalIgnoreCase) >= 0
                || GetLabel(button).IndexOf(nameOrLabel, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string GetLabel(Button button)
        {
            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                return tmp.text;
            }

            Text text = button.GetComponentInChildren<Text>();
            return text != null ? text.text : string.Empty;
        }

        private void Sample()
        {
            float delta = Time.unscaledDeltaTime;
            _frames++;
            _frameTime += delta;
            _worstFrame = Mathf.Max(_worstFrame, delta);
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                string firstFrame = stackTrace.Split('\n').FirstOrDefault() ?? string.Empty;
                _errors.Add($"{type}: {condition} {firstFrame}".Trim());
            }
        }
    }
}
