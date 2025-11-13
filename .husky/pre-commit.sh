#!/bin/sh
set -e

# DVC PATH 보정 (pre-commit 환경 대비)
export PATH="$HOME/.local/bin:/opt/homebrew/bin:$PATH"

# --- DVC 관리 임계값: 100MB (bytes) ---
LIMIT=$((100 * 1024 * 1024))

echo "🔍 Running pre-commit DVC check (Threshold: 100MB)..."

# stat 명령 OS 자동 판별
if stat -c%s . >/dev/null 2>&1; then
    STAT_CMD='stat -c%s'
else
    STAT_CMD='stat -f%z'
fi

rm -rf .dvc/tmp/lock .dvc/tmp/rwlock >/dev/null 2>&1 || true

git diff --cached --name-only -z --diff-filter=AM | while IFS= read -r -d '' file; do

    # 파일 크기 읽기
    size=$($STAT_CMD "$file" 2>/dev/null || echo 0)

    if [ "$size" -gt "$LIMIT" ]; then

        if [ ! -f "$file.dvc" ]; then

            size_mb=$((size / 1024 / 1024))
            echo "📦 File > 100MB → Convert to DVC: $file (${size_mb}MB)"

            # Git 추적 해제
            if git ls-files --error-unmatch "$file" >/dev/null 2>&1; then
                git rm --cached "$file"
            fi

            # DVC 등록
            echo "➡️  dvc add $file"
            dvc add "$file"
            git add "$file.dvc"

            # 메타 파일 처리
            metafile="${file}.meta"
            if [ -f "$metafile" ]; then
                echo "🧩 META Found → Git add: $metafile"
                git add "$metafile"
            fi
        else
            echo "⏭️ Already DVC tracked: $file"
        fi
    fi
done

echo "✅ DVC pre-commit complete."
