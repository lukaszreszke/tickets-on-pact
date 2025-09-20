#!/bin/bash

# Pobierz wszystkie lokalne branche oprócz mastera
branches=$(git for-each-ref --format='%(refname:short)' refs/heads/ | grep -v '^master$')

for branch in $branches; do
  echo "Wypychanie brancha: $branch"
  git push -u origin "$branch"  # wypchnij branch na origin
  
  echo "Tworzenie PR dla brancha: $branch"
  gh pr create -f --head "$branch" --base master
done
