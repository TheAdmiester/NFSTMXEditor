using Microsoft.WindowsAPICodePack.Dialogs;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace NFSTMXEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TMX tmx = new TMX();
        AudioEntry currentEntry;
        string streamType;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void lstAudio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstAudio.SelectedIndex != -1)
            {
                currentEntry = tmx.audioEntries[lstAudio.SelectedIndex];

                lblIndex.Content = currentEntry.index;
                lblType.Content = currentEntry.type;
                lblOffsetR.Content = currentEntry.relativeOffset;
                lblOffsetA.Content = currentEntry.absoluteOffset;

                switch (currentEntry.type)
                {
                    case "GIN":
                        streamType = "EA-XAS 4-bit ADPCM v0";
                        txtMinRpm.IsEnabled = true;
                        txtMaxRpm.IsEnabled = true;
                        txtMinRpm.Text = currentEntry.ginData.minRpm.ToString();
                        txtMaxRpm.Text = currentEntry.ginData.maxRpm.ToString();
                        break;

                    case "SNR":
                        streamType = "EA-XAS 4-bit ADPCM v1";
                        txtMinRpm.IsEnabled = false;
                        txtMaxRpm.IsEnabled = false;
                        txtMinRpm.Text = "";
                        txtMaxRpm.Text = "";
                        break;
                }

                lblStream.Content = string.Format("{0} byte {1} stream", currentEntry.audioData == null ? currentEntry.ginData.audioData.Length : currentEntry.audioData.Length, streamType);
            }
        }

        private void OpenTMX_Click(object sender, RoutedEventArgs e)
        {
            var picker = new CommonOpenFileDialog();
            picker.Filters.Add(new CommonFileDialogFilter("TMX Audio File", "tmx"));

            if (picker.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tmx = tmx.ReadTMX(picker.FileName);

                if (tmx != null)
                {
                    RefreshAudioInfo(tmx);
                    RefreshTMXInfo(tmx.fileName);
                }
            }
        }

        void RefreshAudioInfo(TMX tmx)
        {
            lstAudio.Items.Clear();

            foreach (AudioEntry entry in tmx.audioEntries)
            {
                lstAudio.Items.Add(string.Format("Audio Entry {0}", entry.index + 1));
            }

            lstAudio.SelectedIndex = 0;
        }

        void RefreshTMXInfo(string fileName)
        {
            txtTmxInfo.Text = "";

            if (tmx != null)
            {
                txtTmxInfo.Text += "TMX File Analysis:\n";
                txtTmxInfo.Text += string.Format("File Name: {0}\n", Path.GetFileNameWithoutExtension(fileName));
                txtTmxInfo.Text += string.Format("File Path: {0}\n", fileName);
                txtTmxInfo.Text += string.Format("Header Size: {0} bytes\n", tmx.ginHeaderOffset);
                txtTmxInfo.Text += string.Format("Number of Sounds: {0}\n", tmx.numSounds);
                txtTmxInfo.Text += string.Format("Header Start Offset: {0} (0x{1})\n", tmx.ginHeaderOffset, tmx.ginHeaderOffset.ToString("X"));
                txtTmxInfo.Text += string.Format("Audio Data Start Offset: {0} (0x{1})\n\n", tmx.audioDataOffset, tmx.audioDataOffset.ToString("X"));

                foreach (AudioEntry entry in tmx.audioEntries)
                {
                    txtTmxInfo.Text += string.Format("Audio Entry {0} Info:\n", entry.index + 1);
                    txtTmxInfo.Text += string.Format("Type: {0}\n", entry.type);
                    txtTmxInfo.Text += string.Format("Audio Data Offset (Relative to audio data start): {0} (0x{1})\n", entry.relativeOffset, entry.relativeOffset.ToString("X"));
                    txtTmxInfo.Text += string.Format("Audio Data Offset (Absolute): {0} (0x{1})\n\n", entry.absoluteOffset, entry.absoluteOffset.ToString("X"));
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var picker = new CommonOpenFileDialog();
            picker.Filters.Add(new CommonFileDialogFilter("GIN/SNR Audio", "gin;snr"));

            if (picker.ShowDialog() == CommonFileDialogResult.Ok)
            {
                byte[] data = File.ReadAllBytes(picker.FileName);
                int index = tmx.audioEntries.IndexOf(currentEntry);
                int header;

                using (var stream = new BinaryStream(new MemoryStream(data)))
                {
                    header = stream.ReadInt32();

                    switch ((long)header)
                    {
                        case 0x75736E47L:
                            tmx.audioEntries[index].type = "GIN";
                            tmx.audioEntries[index].ginData = GIN.ReadGIN(data);
                            break;

                        // No actual header so just read the channel/sample rate info, should only ever be one of these for NFS
                        case 0x80BB0004L:
                        case 0x80BB0404L:
                        case 0x44AC0004L:
                        case 0x44AC0404L:
                            tmx.audioEntries[index].type = "SNR";
                            tmx.audioEntries[index].audioData = data;
                            break;
                    }
                }
                

                RefreshAudioInfo(tmx);

                lstAudio.SelectedIndex = index;
            }
        }

        private void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            var saver = new CommonSaveFileDialog();

            txtMinRpm_TextChanged(sender, e);
            txtMaxRpm_TextChanged(sender, e);

            if (saver.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tmx.WriteTMX(saver.FileName);
            }
        }

        private void txtMinRpm_TextChanged(object sender, RoutedEventArgs e)
        {
            int index = tmx.audioEntries.IndexOf(currentEntry);

            if (float.TryParse(txtMinRpm.Text, out float i))
            {
                tmx.audioEntries[index].ginData.minRpm = float.Parse(txtMinRpm.Text);

                RefreshAudioInfo(tmx);

                lstAudio.SelectedIndex = index;
            }
            else
            {
                MessageBox.Show("Invalid Minimum RPM value. Please input a valid number and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                txtMinRpm.Text = tmx.audioEntries[index].ginData.minRpm.ToString();
            }
        }

        private void txtMaxRpm_TextChanged(object sender, RoutedEventArgs e)
        {
            int index = tmx.audioEntries.IndexOf(currentEntry);

            if (float.TryParse(txtMaxRpm.Text, out float i))
            {
                tmx.audioEntries[index].ginData.maxRpm = float.Parse(txtMaxRpm.Text);

                RefreshAudioInfo(tmx);

                lstAudio.SelectedIndex = index;
            }
            else
            {
                MessageBox.Show("Invalid Maximum RPM value. Please input a valid number and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                txtMaxRpm.Text = tmx.audioEntries[index].ginData.maxRpm.ToString();
            }
        }
    }
}
