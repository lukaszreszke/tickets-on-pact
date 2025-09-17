#!/bin/bash
set -euo pipefail

current_branch=$(git branch --show-current)

git checkout master
git pull origin master

for branch in $(git for-each-ref --format='%(refname:short)' refs/heads/ | grep -v '^master$'); do
  echo "=== Rebasing $branch onto master ==="
  git checkout "$branch"
  if ! git rebase master; then
    echo "⚠️ Conflict while rebasing $branch. Resolve manually and run:"
    echo "   git rebase --continue"
    echo "Then restart the script if needed."
    exit 1
  fi
done

git checkout "$current_branch"

echo "✅ All branches rebased onto master"

