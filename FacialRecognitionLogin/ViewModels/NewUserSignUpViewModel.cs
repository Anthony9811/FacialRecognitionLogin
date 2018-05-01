﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.ProjectOxford.Face;

using Xamarin.Forms;

namespace FacialRecognitionLogin
{
    public class NewUserSignUpViewModel : BaseViewModel
    {
        #region Fields
        Guid _facialRecognitionUserGUID;
        string _usernameEntryText, _passwordEntryText, _fontAwesomeLabelText = FontAwesomeIcon.EmptyBox.ToString();
        ICommand _takePhotoButtonCommand, _saveButtonCommand, _cancelButtonCommand;
        #endregion

        #region Events
        public event EventHandler<string> TakePhotoFailed;
        public event EventHandler<string> SaveFailed;
        public event EventHandler SaveSuccessfullyCompleted;
        #endregion

        #region Properties
        public ICommand CancelButtonCommand => _cancelButtonCommand ??
            (_cancelButtonCommand = new Command(async () => await ExecuteCancelButtonCommand()));

        public ICommand TakePhotoButtonCommand => _takePhotoButtonCommand ??
            (_takePhotoButtonCommand = new Command(async () => await ExecuteTakePhotoButtonCommand(UsernameEntryText)));

        public ICommand SaveButtonCommand => _saveButtonCommand ??
            (_saveButtonCommand = new Command(async () => await ExecuteSaveButtonCommand(UsernameEntryText, PasswordEntryText)));

        public string UsernameEntryText
        {
            get => _usernameEntryText;
            set => SetProperty(ref _usernameEntryText, value, () => FontAwesomeLabelText = FontAwesomeIcon.EmptyBox.ToString());
        }

        public string PasswordEntryText
        {
            get => _passwordEntryText;
            set => SetProperty(ref _passwordEntryText, value);
        }

        public string FontAwesomeLabelText
        {
            get => _fontAwesomeLabelText;
            set => SetProperty(ref _fontAwesomeLabelText, value);
        }

        bool IsUsernamePasswordValid => string.IsNullOrWhiteSpace(UsernameEntryText) || string.IsNullOrWhiteSpace(PasswordEntryText);
        #endregion

        #region Methods
        async Task ExecuteTakePhotoButtonCommand(string username)
        {
            if (IsUsernamePasswordValid)
            {
                OnTakePhotoFailed("Username / Password Empty");
                return;
            }

            var photoStream = await PhotoService.GetPhotoStreamFromCamera();
            if (photoStream is null)
                return;

            try
            {
                _facialRecognitionUserGUID = await FacialRecognitionService.AddNewFace(username, photoStream);
                FontAwesomeLabelText = FontAwesomeIcon.CheckedBox.ToString();
            }
            catch (FaceAPIException e)
            {
                OnTakePhotoFailed(e.ErrorMessage);
                FontAwesomeLabelText = FontAwesomeIcon.EmptyBox.ToString();
            }
            catch (Exception e)
            {
                OnTakePhotoFailed(e.Message);
                FontAwesomeLabelText = FontAwesomeIcon.EmptyBox.ToString();
            }
        }

        async Task ExecuteSaveButtonCommand(string username, string password)
        {
            if (FontAwesomeLabelText.Equals(FontAwesomeIcon.EmptyBox.ToString()))
            {
                OnSaveFailed("Photo Required for Facial Recognition");
                return;
            }

            var isUserNamePasswordValid = await DependencyService.Get<ILogin>().SetPasswordForUsername(username, password);
            if (isUserNamePasswordValid)
                OnSaveSuccessfullyCompleted();
            else
                OnSaveFailed("Username / Password Empty");
        }

        async Task ExecuteCancelButtonCommand()
        {
            await WaitForNewUserSignUpPageToDisappear();

            if (!_facialRecognitionUserGUID.Equals(default(Guid)))
                await FacialRecognitionService.RemoveExistingFace(_facialRecognitionUserGUID);
        }

        async Task WaitForNewUserSignUpPageToDisappear() => await Task.Delay(1000);

        void OnTakePhotoFailed(string errorMessage) =>
            TakePhotoFailed?.Invoke(this, errorMessage);

        void OnSaveFailed(string errorMessage) =>
            SaveFailed?.Invoke(this, errorMessage);

        void OnSaveSuccessfullyCompleted() =>
            SaveSuccessfullyCompleted?.Invoke(this, EventArgs.Empty);
        #endregion
    }
}
