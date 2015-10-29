﻿using LSLib.Granny.GR2;
using LSLib.Granny.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConverterApp
{
    public partial class MainForm : Form
    {
        private LSLib.Granny.Model.Root Root;

        public MainForm()
        {
            InitializeComponent();
        }

        private Root LoadGR2(string inPath)
        {
            var root = new LSLib.Granny.Model.Root();
            FileStream fs = new FileStream(inPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
            var gr2 = new LSLib.Granny.GR2.GR2Reader(fs);
            gr2.Read(root);
            root.PostLoad();
            fs.Close();
            fs.Dispose();
            return root;
        }

        private void UpdateExportableObjects()
        {
            exportableObjects.Items.Clear();

            if (Root.Models != null)
            {
                foreach (var model in Root.Models)
                {
                    var item = new ListViewItem(new string[] { model.Name, "Model" });
                    item.Checked = true;
                    exportableObjects.Items.Add(item);
                }
            }

            if (Root.Skeletons != null)
            {
                foreach (var skeleton in Root.Skeletons)
                {
                    var item = new ListViewItem(new string[] { skeleton.Name, "Skeleton" });
                    item.Checked = true;
                    exportableObjects.Items.Add(item);
                }
            }

            if (Root.Animations != null)
            {
                foreach (var animation in Root.Animations)
                {
                    var item = new ListViewItem(new string[] { animation.Name, "Animation" });
                    item.Checked = true;
                    exportableObjects.Items.Add(item);
                }
            }
        }

        private void UpdateResourceFormats()
        {
            resourceFormats.Items.Clear();

            if (Root.Meshes != null)
            {
                foreach (var mesh in Root.Meshes)
                {
                    var item = new ListViewItem(new string[] { mesh.Name, "Mesh", "Automatic" });
                    resourceFormats.Items.Add(item);
                }
            }

            if (Root.TrackGroups != null)
            {
                foreach (var trackGroup in Root.TrackGroups)
                {
                    foreach (var track in trackGroup.TransformTracks)
                    {
                        if (track.PositionCurve.CurveData.NumKnots() > 2)
                        {
                            var item = new ListViewItem(new string[] { track.Name, "Position Track", "Automatic" });
                            resourceFormats.Items.Add(item);
                        }

                        if (track.OrientationCurve.CurveData.NumKnots() > 2)
                        {
                            var item = new ListViewItem(new string[] { track.Name, "Rotation Track", "Automatic" });
                            resourceFormats.Items.Add(item);
                        }

                        if (track.ScaleShearCurve.CurveData.NumKnots() > 2)
                        {
                            var item = new ListViewItem(new string[] { track.Name, "Scale/Shear Track", "Automatic" });
                            resourceFormats.Items.Add(item);
                        }
                    }
                }
            }
        }

        private void UpdateInputState()
        {
            var skinned = (Root.Skeletons != null && Root.Skeletons.Count > 0);
            conformToSkeleton.Enabled = skinned;
            if (!skinned)
            {
                conformToSkeleton.Checked = false;
            }

            buildDummySkeleton.Enabled = !skinned;
            if (skinned)
            {
                buildDummySkeleton.Checked = false;
            }

            UpdateExportableObjects();
            UpdateResourceFormats();

            saveOutputBtn.Enabled = true;
        }

        private void LoadFile(string inPath)
        {
            var isGR2 = inPath.Length > 4 && inPath.Substring(inPath.Length - 4).ToLower() == ".gr2";

            if (isGR2)
            {
                Root = LoadGR2(inPath);
            }
            else
            {
                Root = new LSLib.Granny.Model.Root();
                Root.ImportFromCollada(inPath);
            }

            UpdateInputState();
        }

        private void UpdateExporterSettings(ExporterOptions settings)
        {
            settings.InputPath = inputFileDlg.FileName;
            if (settings.InputPath.Substring(settings.InputPath.Length - 4).ToLower() == ".gr2")
            {
                settings.InputFormat = ExportFormat.GR2;
            }
            else
            {
                settings.InputFormat = ExportFormat.DAE;
            }

            settings.OutputPath = outputFileDlg.FileName;
            if (settings.OutputPath.Substring(settings.OutputPath.Length - 4).ToLower() == ".gr2")
            {
                settings.OutputFormat = ExportFormat.GR2;
            }
            else
            {
                settings.OutputFormat = ExportFormat.DAE;
            }

            settings.ExportNormals = exportNormals.Checked;
            settings.ExportTangents = exportTangents.Checked;
            settings.ExportUVs = exportUVs.Checked;
            settings.RecalculateNormals = recalculateNormals.Checked;
            settings.RecalculateTangents = recalculateTangents.Checked;
            settings.RecalculateIWT = recalculateJointIWT.Checked;
            settings.CompactIndices = use16bitIndex.Checked;
            settings.DeduplicateVertices = deduplicateVertices.Checked;
            settings.DeduplicateUVs = filterUVs.Checked;
            settings.ApplyBasisTransforms = applyBasisTransforms.Checked;
            settings.UseObsoleteVersionTag = forceLegacyVersion.Checked;

            if (conformToSkeleton.Checked && conformantSkeletonPath.Text.Length > 0)
            {
                settings.ConformSkeletonsPath = conformantSkeletonPath.Text;
            }
            else
            {
                settings.ConformSkeletonsPath = null;
            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void inputFileBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = inputFileDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                inputPath.Text = inputFileDlg.FileName;
            }
        }

        private void loadInputBtn_Click(object sender, EventArgs e)
        {
            try
            {
                LoadFile(inputPath.Text);
            }
            catch (ParsingException exc)
            {
                MessageBox.Show("Import failed!\r\n\r\n" + exc.Message, "Import Failed");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Internal error!\r\n\r\n" + exc.ToString(), "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void recalculateJointIWT_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void conformToSkeleton_CheckedChanged(object sender, EventArgs e)
        {
            conformantSkeletonPath.Enabled = conformToSkeleton.Checked;
            conformantSkeletonBrowseBtn.Enabled = conformToSkeleton.Checked;
        }

        private void outputFileBrowserBtn_Click(object sender, EventArgs e)
        {
            var result = outputFileDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                outputPath.Text = outputFileDlg.FileName;
            }
        }

        private void conformantSkeletonBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = conformSkeletonFileDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                conformantSkeletonPath.Text = conformSkeletonFileDlg.FileName;
            }
        }

        private void saveOutputBtn_Click(object sender, EventArgs e)
        {
            var exporter = new Exporter();
            UpdateExporterSettings(exporter.Options);
            try
            {
                exporter.Export();
                MessageBox.Show("Export completed successfully.");
            }
            catch (ExportException exc)
            {
                MessageBox.Show("Export failed!\r\n\r\n" + exc.Message, "Export Failed");
            }
            catch (ParsingException exc)
            {
                MessageBox.Show("Export failed!\r\n\r\n" + exc.Message, "Export Failed");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Internal error!\r\n\r\n" + exc.ToString(), "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}