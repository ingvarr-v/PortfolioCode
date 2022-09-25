using System.Collections;
using System.Collections.Generic;
using Helmeton.Base;
using Helmeton.Experimental.VariablesSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Helmeton.Experimental.ScenarioSystem2
{
    public class ScenarioPlayer : MonoBehaviour
    {
        public event ScenarioEventHandler CurrentScenarioChanged;
        public event StepEventHandler CurrentStepChanged;

        public UnityEvent Started;
        public UnityEvent Stopped;

        [SerializeField]
        private bool _playOnAwake = false;
        public bool IsRandom = false;
        [SerializeField]
        private bool _isLoop = false;
        [SerializeField]
        private List<Scenario> _scenarios;

        [Header("Scenarios Data")]

        private Coroutine _playRoutine;

        private Scenario _currentScenario;
        public Scenario CurrentScenario
        {
            get => _currentScenario;
            private set
            {
                _currentScenario = value;
                _currentScenarioName = value.Name;
                OnScenarioChanged(new ScenarioEventArgs(value.Name, value.Description));
            }
        }

        private string _currentScenarioName;

        private int _currentIndex;

        private List<GameObject> _scenariosBuffer;

        private List<int> _scenarioOrderIndexes;

        private bool _isPlaying;
        public bool IsPlaying => _isPlaying;

        public TextAsset GlobalVariablesData;
        private VariablesCatalogContainer _globalVariables;

        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            if (_playOnAwake) Play();
        }

        private void Init()
        {
            _currentIndex = 0;

            _scenariosBuffer = new List<GameObject>(_scenarios.Count);

            InitPlaybackOrder(IsRandom);

            if (GlobalVariablesData != null)
                _globalVariables = VariableSaveUtility.LoadCatalogContainer(GlobalVariablesData);
        }

        private void InitPlaybackOrder(bool randomOrder)
        {
            _scenarioOrderIndexes = new List<int>(_scenarios.Count);

            for (int i = 0; i < _scenarios.Count; i++)
                _scenarioOrderIndexes.Add(i);

            if (randomOrder) _scenarioOrderIndexes.Shuffle();
        }

        private void OnStarted()
        {
            Started.Invoke();

            _isPlaying = true;
        }

        private void OnStopped()
        {
            Stopped.Invoke();

            _isPlaying = false;
        }

        public void Play()
        {
            _playRoutine = StartCoroutine(PlayRoutine(_currentIndex));

            Debug.Log($"<color=green>{name}: Start playing</color>");
        }

        public void Stop()
        {
            OnStopped();

            StopPlayRoutine();

            _currentIndex = 0;

            Debug.Log($"<color=red>{name}: Scenario playing terminated</color>");
        }

        public void Pause()
        {
            PauseCurrentScenario();
            Time.timeScale = Time.timeScale == 0 ? 1 : 0;
        }

        private void PauseCurrentScenario()
        {
            if (_currentScenario != null)
            {
                _currentScenario.IsPaused = !_currentScenario.IsPaused;
                if (SoundSystem.Instance != null)
                    SoundSystem.Instance.PauseVoice(_currentScenario.IsPaused);
            }
        }

        public void PlayPrevious()
        { }

        public void PlayNext()
        { }

        private void StopPlayRoutine()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
                CurrentScenario.StopScenario();
            }
        }

        private void RemoveAllScenarios()
        {
            foreach (var scenario in _scenariosBuffer)
            {
                Destroy(scenario.gameObject);
            }

            _scenariosBuffer.Clear();
            _scenarioOrderIndexes.Clear();
        }

        private IEnumerator PlayRoutine(int startIndex)
        {
            OnStarted();

            do
            {
                for (int i = startIndex; i < _scenarios.Count; i++)
                {
                    _currentIndex = i;

                    CurrentScenario = _scenarios[_scenarioOrderIndexes[i]];
                    CurrentScenario.StepChanged += OnCurrentStepChanged;

                    yield return null;

                    CurrentScenario.Play(_globalVariables);

                    yield return new WaitWhile(() => CurrentScenario.InProgress);

                    CurrentScenario.StepChanged -= OnCurrentStepChanged;
                }
            }
            while (_isLoop);

            _playRoutine = null;

            OnStopped();

            Debug.Log($"<color=green>{name}: Stop playing</color>");
        }

        private void OnCurrentStepChanged(object sender, StepEventArgs e)
        {
            CurrentStepChanged?.Invoke(this, e);
        }

        private void OnScenarioChanged(ScenarioEventArgs e)
        {
            CurrentScenarioChanged?.Invoke(this, e);
        }

        public void SetScenarios(List<Scenario> scenarios)
        {
            _scenarios.Clear();
            foreach (var scenario in scenarios)
                _scenarios.Add(scenario);
        }
    }
}
