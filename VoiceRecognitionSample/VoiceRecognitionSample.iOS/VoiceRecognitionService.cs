using AVFoundation;
using Foundation;
using Speech;
using System;
using VoiceRecognitionSample.Models;
using Xamarin.Forms;

[assembly: Dependency(typeof(VoiceRecognitionSample.iOS.VoiceRecognitionService))]
namespace VoiceRecognitionSample.iOS
{
    public class VoiceRecognitionService : BindableBase, IVoiceRecognitionService
    {
        #region Properties

        // 音声認識の実行状況（実行中の間のみtrueを返す）
        private bool _isRecognizing;
        public bool IsRecognizing
        {
            get => _isRecognizing;
            set => base.SetProperty<bool>(ref _isRecognizing, value);
        }

        // 音声認識の結果テキスト
        private string _recognizedText;
        public string RecognizedText
        {
            get => _recognizedText ?? string.Empty;
            set => base.SetProperty(ref _recognizedText, value);
        }

        #endregion

        #region Variables

        // 音声認識に必要な諸々のクラスのインスタンス。
        private AVAudioEngine _audioEngine;
        private SFSpeechRecognizer _speechRecognizer;
        private SFSpeechAudioBufferRecognitionRequest _recognitionRequest;
        private SFSpeechRecognitionTask _recognitionTask;

        #endregion

        #region Public Methods

        // 音声認識の開始処理
        public void StartRecognizing()
        {
            RecognizedText = string.Empty;
            IsRecognizing = true;

            // 音声認識の許可をユーザーに求める。
            SFSpeechRecognizer.RequestAuthorization((SFSpeechRecognizerAuthorizationStatus status) =>
            {
                switch (status)
                {
                    case SFSpeechRecognizerAuthorizationStatus.Authorized:
                        // 音声認識がユーザーに許可された場合、必要なインスタンスを生成した後に音声認識の本処理を実行する。
                        // SFSpeechRecognizerのインスタンス生成時、コンストラクタの引数でlocaleを指定しなくても、
                        // 端末の標準言語が日本語なら日本語は問題なく認識される。
                        _audioEngine = new AVAudioEngine();
                        _speechRecognizer = new SFSpeechRecognizer();
                        _recognitionRequest = new SFSpeechAudioBufferRecognitionRequest();
                        StartRecognitionSession();
                        break;
                    default:
                        // 音声認識がユーザーに許可されなかった場合、処理を終了する。
                        return;
                }
            }
            );
        }

        // 音声認識の停止処理
        public void StopRecognizing()
        {
            try
            {
                _audioEngine?.Stop();
                _recognitionTask?.Cancel();
                _recognitionRequest?.EndAudio();
                IsRecognizing = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Private Methods

        // 音声認識の本処理
        private void StartRecognitionSession()
        {
            // 音声認識のパラメータ設定と認識開始。ここのパラメータはおまじない。
            _audioEngine.InputNode.InstallTapOnBus(
                bus: 0,
                bufferSize: 1024,
                format: _audioEngine.InputNode.GetBusOutputFormat(0),
                tapBlock: (buffer, when) => { _recognitionRequest?.Append(buffer); }
            );
            _audioEngine?.Prepare();
            NSError error = null;
            _audioEngine?.StartAndReturnError(out error);
            if (error != null)
            {
                Console.WriteLine(error);
                return;
            }

            try
            {
                if (_recognitionTask?.State == SFSpeechRecognitionTaskState.Running)
                {
                    // 音声認識が実行中に音声認識開始処理が呼び出された場合、実行中だった音声認識を中断する。
                    _recognitionTask.Cancel();
                }

                _recognitionTask = _speechRecognizer.GetRecognitionTask(_recognitionRequest,
                    (SFSpeechRecognitionResult result, NSError err) =>
                    {
                        if (result == null)
                        {
                            // iOS Simulator等、端末が音声認識に対応していない場合はここに入る。
                            StopRecognizing();
                            return;
                        }

                        if (err != null)
                        {
                            Console.WriteLine(err);
                            StopRecognizing();
                            return;
                        }

                        if ((result.BestTranscription != null) && (result.BestTranscription.FormattedString != null))
                        {
                            // 音声を認識できた場合、認識結果を更新する。
                            RecognizedText = result.BestTranscription.FormattedString;
                        }

                        if (result.Final)
                        {
                            // 音声が認識されなくなって時間が経ったら音声認識を打ち切る。
                            StopRecognizing();
                            return;
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion
    }

}