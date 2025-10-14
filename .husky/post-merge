#!/bin/sh
# post-merge: git pull/merge 후 dvc pull 실행

echo "📥 Running dvc pull after merge/pull..."

rm -rf .dvc/tmp/lock .dvc/tmp/rwlock 2>/dev/null || true
dvc pull
dvc checkout -f

echo "✅ dvc pull completed"
exit 0
