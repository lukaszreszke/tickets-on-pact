
#!/bin/bash

branches=$(git for-each-ref --format='%(refname:short)' refs/heads/ | grep -v '^master$')

for branch in $branches; do
  echo "Tworzenie PR dla brancha: $branch"
  
  git checkout $branch
  
  gh pr create -f
done

git checkout master
