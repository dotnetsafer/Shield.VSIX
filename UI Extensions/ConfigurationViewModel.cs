﻿using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;
using EnvDTE80;
using ShieldVSExtension.Configuration;
using ShieldVSExtension.Helpers;

namespace ShieldVSExtension.UI_Extensions
{
    public sealed class ConfigurationViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Design-Time Ctor
#if DEBUG
        [Obsolete("For design-time only")]
        public ConfigurationViewModel()
        {
            TargetDirectory = "test";

            var p1 = new ProjectViewModel();
            p1.Name = "proj1";
            p1.IsEnabled = true;
            p1.FolderName = "common";
            p1.Files.Add(new ProjectFileViewModel { FileName = "file1" });
            p1.Files.Add(new ProjectFileViewModel { FileName = "file2" });
            p1.Files.Add(new ProjectFileViewModel { FileName = "file3" });
            p1.OutputFullPath = "C:\\Windows";
            p1.TargetDirectory = "C:\\Temp";

            var p2 = new ProjectViewModel();
            p2.Name = "proj2";
            p2.IsEnabled = false;
            p1.FolderName = "common/lib";
            p2.Files.Add(new ProjectFileViewModel { FileName = "file4" });
            p2.Files.Add(new ProjectFileViewModel { FileName = "file5" });
            p2.Files.Add(new ProjectFileViewModel { FileName = "file6" });

            var p3 = new ProjectViewModel();
            p2.Name = "proj3";
            p2.IsEnabled = true;
            p1.FolderName = "common";
            p3.OutputFullPath = "C:\\Windows";

            Projects = new Collection<ProjectViewModel>
            {
                p1,
                p2,
                p3
            };

            SelectedProject = p1;

            _solutionConfiguration = new Configuration.SolutionConfiguration();
        }
#endif
        #endregion

        #region TargetDirectory Property

        private string _targetDirectory;

        public string TargetDirectory
        {
            get { return _targetDirectory; }
            set
            {
                if (_targetDirectory == value)
                    return;

                _targetDirectory = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region ProjectPreset Property

        private ProjectPreset _projectPreset;

        public ProjectPreset ProjectPreset
        {
            get { return _projectPreset; }
            set
            {
                if (_projectPreset == value)
                    return;

                _projectPreset = value;
                OnPropertyChanged();
            }
        }

        #endregion

        

        #region CreateShieldProjectIfNotExists Property

        private bool _createShieldProjectIfNotExists;

        public bool CreateShieldProjectIfNotExists
        {
            get { return _createShieldProjectIfNotExists; }
            set
            {
                if (_createShieldProjectIfNotExists == value)
                    return;

                _createShieldProjectIfNotExists = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region FindCustomConfigurationFile Property

        private bool _findCustomConfigurationFile;

        public bool FindCustomConfigurationFile
        {
            get { return _findCustomConfigurationFile; }
            set
            {
                if (_findCustomConfigurationFile == value)
                    return;

                _findCustomConfigurationFile = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region IsValidClient Property

        private bool _isValidClient;

        public bool IsValidClient
        {
            get { return _isValidClient; }
            set
            {
                if (_isValidClient == value)
                    return;

                _isValidClient = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region SelectedProject Property

        private ProjectViewModel _selectedProject;

        public ProjectViewModel SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                if (_selectedProject == value)
                    return;

                _selectedProject = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region SelectedProjects Property

        public ICollection<ProjectViewModel> SelectedProjects { get; }

        #endregion

        #region Projects Property

        public ICollection<ProjectViewModel> Projects { get; }

        #endregion

        #region ShieldProjectName Property

        private string _shieldProjectName;

        public string ShieldProjectName
        {
            get { return _shieldProjectName; }
            set
            {
                if (_shieldProjectName == value)
                    return;

                _shieldProjectName = value;
                OnPropertyChanged();
            }
        }

        #endregion

        private ObservableCollection<ProjectPreset> _projectPresets;

        public ObservableCollection<ProjectPreset> ProjectPresets
        {
            get { return _projectPresets; }
            set { _projectPresets = value; }
        }


        private readonly Configuration.SolutionConfiguration _solutionConfiguration;

        public ConfigurationViewModel(DTE2 dte, Configuration.SolutionConfiguration solutionConfiguration)
        {
            _solutionConfiguration = solutionConfiguration;

            var projects = new List<ProjectViewModel>();
            var dteProjects = dte.Solution.GetProjects()
                .OrderBy(p => Path.GetDirectoryName(p.UniqueName))
                .ThenBy(p => p.UniqueName)
                .ToArray();

            foreach (var dteProject in dteProjects)
            {
                try
                {
                    var projectConfiguration = solutionConfiguration.Projects.FirstOrDefault(p => p.ProjectName == dteProject.UniqueName) ??
                                               new ProjectConfiguration();

                    var projectViewModel = new ProjectViewModel(dteProject, projectConfiguration.Files)
                    {
                        IsEnabled = projectConfiguration.IsEnabled,
                        IncludeSubDirectories = projectConfiguration.IncludeSubDirectories,
                        TargetDirectory = projectConfiguration.TargetDirectory,
                        InheritFromProject = projectConfiguration.InheritFromProject,
                    };
                    projects.Add(projectViewModel);
                }
                catch
                {
                }
            }

            Projects = projects;
            ProjectPresets = new ObservableCollection<ProjectPreset>
            {
                new ProjectPreset {Id=1, Name="Maximum"}
                , new ProjectPreset {Id=2,Name="Balance"}
                , new ProjectPreset {Id=3, Name="Optimization"}
            };
            TargetDirectory = solutionConfiguration.TargetDirectory;
            CreateShieldProjectIfNotExists = solutionConfiguration.CreateShieldProjectIfNotExists;
            FindCustomConfigurationFile = solutionConfiguration.FindCustomConfigurationFile;
            ProjectPreset = solutionConfiguration.ProjectPreset;
            ShieldProjectName = solutionConfiguration.ShieldProjectName;
            SelectedProjects = new ObservableCollection<ProjectViewModel>();
            IsValidClient = false;

            if (dte.Solution.SolutionBuild.StartupProjects is object[] startupProjects)
            {
                var startupProject = startupProjects.OfType<string>().FirstOrDefault();
                if (startupProject != null)
                    SelectedProject = Projects.FirstOrDefault(p => p.Project.UniqueName == startupProject);
            }

            if (SelectedProject == null)
                SelectedProject = Projects.FirstOrDefault();
        }


        public void Enable(bool isEnabled)
        {
            foreach (var projectViewModel in SelectedProjects)
                projectViewModel.IsEnabled = isEnabled;
        }

        public void IncludeSubDirectories(bool include)
        {
            foreach (var projectViewModel in SelectedProjects)
                projectViewModel.IncludeSubDirectories = include;
        }

        public void AddOutput(string fileExtension)
        {
            foreach (var item in SelectedProjects)
            {
                var targetFileName = item.Name + fileExtension;

                if (!File.Exists(Path.Combine(item.OutputFullPath, targetFileName)))
                    continue;

                if (item.Files.Any(p => String.Equals(p.FileName, targetFileName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                item.Files.Add(new ProjectFileViewModel(targetFileName));
            }
        }

        public void AddOutputByPattern(string searchPattern)
        {
            foreach (var item in SelectedProjects)
            {
                if (item.Files.Any(p => String.Equals(p.FileName, searchPattern, StringComparison.OrdinalIgnoreCase)))
                    continue;

                item.Files.Add(new ProjectFileViewModel(searchPattern));
            }
        }

        public void ClearFiles()
        {
            foreach (var projectViewModel in SelectedProjects)
                projectViewModel.Files.Clear();
        }

        public void ClearTargetDirectory()
        {
            foreach (var item in SelectedProjects)
                item.TargetDirectory = null;
        }


        public void Save()
        {
            _solutionConfiguration.TargetDirectory = TargetDirectory;
            _solutionConfiguration.ShieldProjectName = ShieldProjectName;
            _solutionConfiguration.CreateShieldProjectIfNotExists = CreateShieldProjectIfNotExists;
            _solutionConfiguration.FindCustomConfigurationFile = FindCustomConfigurationFile;
            _solutionConfiguration.ProjectPreset = ProjectPreset;
            _solutionConfiguration.Projects.Clear();

            foreach (var projectViewModel in Projects)
            {
                var projectConfiguration = new ProjectConfiguration
                {
                    IsEnabled = projectViewModel.IsEnabled,
                    ProjectName = projectViewModel.Project.UniqueName,
                    IncludeSubDirectories = projectViewModel.IncludeSubDirectories,
                    TargetDirectory = projectViewModel.TargetDirectory,
                    InheritFromProject = projectViewModel.InheritFromProject,
                };

                foreach (var projectFileViewModel in projectViewModel.Files)
                    projectConfiguration.Files.Add(projectFileViewModel.FileName);

                _solutionConfiguration.Projects.Add(projectConfiguration);
            }
        }

        public class ProjectViewModel : INotifyPropertyChanged
        {
            #region INotifyPropertyChanged

            public event PropertyChangedEventHandler PropertyChanged = delegate { };

            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion

            #region IsEnabled Property

            private bool _isEnabled;

            public bool IsEnabled
            {
                get { return _isEnabled; }
                set
                {
                    if (_isEnabled == value)
                        return;

                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }

            #endregion

            #region IncludeSubDirectories Property

            private bool _includeSubDirectories;

            public bool IncludeSubDirectories
            {
                get { return _includeSubDirectories; }
                set
                {
                    if (_includeSubDirectories == value)
                        return;

                    _includeSubDirectories = value;
                    OnPropertyChanged();
                }
            }

            #endregion

            #region InheritFromProject Property

            private bool _inheritFromProject;

            public bool InheritFromProject
            {
                get { return _inheritFromProject; }
                set
                {
                    if (_inheritFromProject == value)
                        return;

                    _inheritFromProject = value;
                    OnPropertyChanged();
                }
            }

            #endregion


            #region TargetDirectory Property

            private string _targetDirectory;

            public string TargetDirectory
            {
                get { return _targetDirectory; }
                set
                {
                    if (_targetDirectory == value)
                        return;

                    _targetDirectory = value;
                    OnPropertyChanged();
                }
            }

            #endregion


            public string Name { get; internal set; }

            public string FolderName { get; internal set; }

            public string OutputFullPath { get; internal set; }

            public ObservableCollection<ProjectFileViewModel> Files { get; }

            internal Project Project { get; }

            public ProjectViewModel()
            {
                Files = new ObservableCollection<ProjectFileViewModel>();
            }

            public ProjectViewModel(Project project, IEnumerable<string> files)
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

                Project = project;
                Name = Path.GetFileNameWithoutExtension(project.UniqueName);
                FolderName = Path.GetDirectoryName(project.UniqueName);
                OutputFullPath = project.GetFullOutputPath();
                Files = new ObservableCollection<ProjectFileViewModel>(files.Select(p => new ProjectFileViewModel(p)).ToList());
            }
        }

        public class ProjectFileViewModel
        {
            public string FileName { get; set; }

            public ProjectFileViewModel()
            {
            }

            public ProjectFileViewModel(string fileName)
            {
                FileName = fileName;
            }
        }
    }


}
