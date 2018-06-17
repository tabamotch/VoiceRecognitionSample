using System;
using System.ComponentModel;
using Reactive.Bindings;
using VoiceRecognitionSample.Models;

namespace VoiceRecognitionSample.ViewModels
{
    public class MainPageViewModel
    {
        // 音声認識の開始・停止ボタンのテキスト
        private const string BUTTON_TEXT_START = "開始";
        private const string BUTTON_TEXT_STOP = "停止";

        // 音声認識の結果テキスト
        private ReactiveProperty<string> RecognizedText { get; } = new ReactiveProperty<string>();

        // 音声認識の開始・停止ボタンの表記
        private ReactiveProperty<string> VoiceRecognitionButtonText { get; } = new ReactiveProperty<string>(BUTTON_TEXT_START);

        // 音声認識を実行中かどうか（trueなら実行中）
        private ReactiveProperty<bool> IsRecognizing { get; } = new ReactiveProperty<bool>(false);

        // 音声認識サービス
        private readonly IVoiceRecognitionService _voiceRecognitionService;

        // 音声認識サービスの処理の呼び出し用コマンド
        private ReactiveCommand VoiceRecognitionCommand { get; } = new ReactiveCommand();

        public MainPageViewModel(IVoiceRecognitionService voiceRecognitionService)
        {
            _voiceRecognitionService = voiceRecognitionService;

            // 音声認識サービスのプロパティが変更されたときに実行する処理を設定する。
            _voiceRecognitionService.PropertyChanged += VoiceRecognitionServicePropertyChanged;

            // IsRecognizingプロパティが変更されたときの処理
            IsRecognizing.Subscribe(_ =>
            {
                VoiceRecognitionButtonText.Value =
                    IsRecognizing.Value ? BUTTON_TEXT_STOP : BUTTON_TEXT_START;
            });

            // 音声認識サービスの処理本体をコマンドに紐付ける。
            //VoiceRecognitionCommand = new DelegateCommand(executeVoiceRecognition);
            VoiceRecognitionCommand.Subscribe(ExecuteVoiceRecognition);
        }

        // 音声認識サービスのプロパティ変更時にトリガーされるイベントの実処理
        private void VoiceRecognitionServicePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "RecognizedText")
            {
                // 音声の認識結果テキストの変更がトリガーになった場合、そのテキストをViewModelに取得する。
                RecognizedText.Value = _voiceRecognitionService.RecognizedText;

                //
                // 音声の認識結果テキストを使って何か処理をしたい場合、
                // ここに処理(または処理の呼び出し)を書けばOK。
                //
            }
            if (args.PropertyName == "IsRecognizing")
            {
                // 音声認識の実行状況変更がトリガーになった場合、その実行状況をViewModelに取得する。
                IsRecognizing.Value = _voiceRecognitionService.IsRecognizing;
            }
        }


        // 音声認識サービス呼び出し用ボタンのコマンドの実処理
        private void ExecuteVoiceRecognition()
        {
            if (IsRecognizing.Value)
            {
                // 音声認識を実行中の場合、「停止」ボタンとして機能させる。
                _voiceRecognitionService.StopRecognizing();
            }
            else
            {
                // 音声認識が停止中の場合、「開始」ボタンとして機能させる。
                _voiceRecognitionService.StartRecognizing();
            }
        }

    }
}