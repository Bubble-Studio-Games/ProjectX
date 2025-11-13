#!/bin/sh
set -e

# --- DVC 관리 임계값: 100MB (bytes) ---
LIMIT=$((100 * 1024 * 1024))
found_large_file_to_convert=false

echo "🔍 Running pre-push DVC check (Threshold: 100MB)..."

# PATH 보정 (pre-push는 PATH가 축소되어 있음)
export PATH="$HOME/.local/bin:/opt/homebrew/bin:$PATH"

# OS별 stat 명령 분기 (Linux vs macOS)
if stat -c%s . >/dev/null 2>&1; then
    STAT_CMD='stat -c%s'
else
    STAT_CMD='stat -f%z'
fi

rm -rf .dvc/tmp/lock .dvc/tmp/rwlock >/dev/null 2>&1 || true

# Push로 넘어가는 커밋 범위 읽기
while read local_ref local_sha remote_ref remote_sha; do
    
    if [ "$local_sha" != "0000000000000000000000000000000000000000" ]; then
        
        git diff --name-only --diff-filter=AM "$remote_sha...$local_sha" | while read file; do

            [[ "$file" == *.meta ]] && continue
            [[ "$file" == *.dvc ]] && continue
            
            if [ -f "$file" ]; then
                
                size=$($STAT_CMD "$file" 2>/dev/null || echo 0)

                if [ "$size" -gt "$LIMIT" ]; then
                    
                    if [ ! -f "$file.dvc" ]; then
                        
                        size_mb=$((size / 1024 / 1024))
                        echo "⚠️  Large file detected: $file (${size_mb}MB) -> converting to DVC"

                        if git ls-files --error-unmatch "$file" >/dev/null 2>&1; then
                            echo "ℹ️  Removing from git index: $file"
                            git rm --cached "$file"
                        fi

                        echo "➡️  dvc add $file"
                        dvc add "$file"
                        git add "$file.dvc"

                        metafile="${file}.meta"
                        if [ -f "$metafile" ]; then
                            echo "🧩 META detected → Git add: $metafile"
                            git add "$metafile"
                        fi

                        found_large_file_to_convert=true
                    fi
                fi
            fi
        done
    fi
done

# 실제 변환 발생한 경우 push 중단하고 commit 하게 유도
if [ "$found_large_file_to_convert" = true ]; then
    echo ""
    echo "❌ Push aborted: Large file(s) were automatically moved to DVC."
    echo "   👉 Run: git commit -m \"Move large files to DVC\""
    echo "   그리고 다시 push 하세요."
    exit 1
fi

# large 파일 변경 없음 → dvc push 실행
echo "🚀 Running DVC push..."
dvc push
echo "✅ DVC push completed."

exit 0
