using System;
using System.IO;
using Microsoft.TeamFoundation.VersionControl.Client;
using CheckinOptions=Sep.Git.Tfs.Commands.CheckinOptions;

namespace Sep.Git.Tfs.Core
{
    public class TfsWorkspace : ITfsWorkspace
    {
        private readonly Workspace _workspace;
        private readonly string _localDirectory;
        private readonly TextWriter _stdout;
        private readonly TfsChangesetInfo _contextVersion;
        private readonly IGitTfsRemote _remote;
        private readonly CheckinOptions _checkinOptions;

        public TfsWorkspace(Workspace workspace, string localDirectory, TextWriter stdout, TfsChangesetInfo contextVersion, IGitTfsRemote remote, CheckinOptions checkinOptions)
        {
            _workspace = workspace;
            _contextVersion = contextVersion;
            _remote = remote;
            _checkinOptions = checkinOptions;
            _localDirectory = localDirectory;
            _stdout = stdout;
        }

        public void Shelve(string shelvesetName)
        {
            var pendingChanges = _workspace.GetPendingChanges();

            if (pendingChanges.Length == 0)
            {
                _stdout.WriteLine(" nothing to shelve");
            }
            else
            {
                var shelveset = new Shelveset(_workspace.VersionControlServer, shelvesetName, _workspace.OwnerName);
                shelveset.Comment = _checkinOptions.CheckinComment;
                _workspace.Shelve(shelveset, _workspace.GetPendingChanges(), _checkinOptions.Force ? ShelvingOptions.Replace : ShelvingOptions.None);
            }
        }

        public string GetLocalPath(string path)
        {
            return Path.Combine(_localDirectory, path);
        }

        public void Add(string path)
        {
            _stdout.WriteLine(" add " + path);
            var added = _workspace.PendAdd(GetLocalPath(path));
            if (added != 1) throw new Exception("One item should have been added, but actually added " + added + " items.");
        }

        public void Edit(string path)
        {
            _stdout.WriteLine(" edit " + path);
            GetFromTfs(path);
            var edited = _workspace.PendEdit(GetLocalPath(path));
            if(edited != 1) throw new Exception("One item should have been edited, but actually edited " + edited + " items.");
        }

        public void Delete(string path)
        {
            _stdout.WriteLine(" delete " + path);
            GetFromTfs(path);
            var deleted = _workspace.PendDelete(GetLocalPath(path));
            if (deleted != 1) throw new Exception("One item should have been deleted, but actually deleted " + deleted + " items.");
        }

        private void GetFromTfs(string path)
        {
            var item = new ItemSpec(_remote.TfsRepositoryPath + "/" + path, RecursionType.None);
            _workspace.Get(new GetRequest(item, (int) _contextVersion.ChangesetId),
                           GetOptions.Overwrite | GetOptions.GetAll);
        }
    }
}