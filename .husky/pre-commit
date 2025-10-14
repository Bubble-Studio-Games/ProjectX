#!/bin/sh
set -e

LIMIT=$((100 * 1024 * 1024))  # 100MB (bytes)

echo "🔍 Running pre-commit DVC check..."

echo "🧾 Checking DVC cache consistency..."
dvc status -c || echo "⚠️ Warning: DVC cache inconsistency detected."

rm -rf .dvc/tmp/lock .dvc/tmp/rwlock >/dev/null 2>&1 || true

# --- 변경된 파일만 검사 (스테이징된 대상)
git diff --cached --name-only -z --diff-filter=AM | while IFS= read -r -d '' file; do
    # Unity 에셋만 검사
    [[ "$file" != Assets/* ]] && continue

    # 확장자 추출
    ext="${file##*.}"
    process=false

    # 확장자 규칙 적용
    case "$ext" in
        fbx|anim|wav|mp4|psd|exr|hdr|tga)  # 무조건 DVC
            process=true ;;
        png|jpg|obj|blend|prefab)          # 100MB 초과만 DVC
            size=$(stat -c%s "$file" 2>/dev/null || echo 0)
            [ $size -gt $LIMIT ] && process=true ;;
    esac

    # --- DVC 등록 처리
    if [ "$process" = true ]; then
        if [ ! -f "$file.dvc" ]; then
            echo "📦 DVC add: $file"
            dvc add "$file" >/dev/null 2>&1 && git add "$file.dvc"

            # --- 해당 에셋의 .meta도 함께 추가 ---
            metafile="${file}.meta"
            if [ -f "$metafile" ] && [ ! -f "$metafile.dvc" ]; then
                echo "🧩 Matching META found → DVC add: $metafile"
                dvc add "$metafile" >/dev/null 2>&1 && git add "$metafile.dvc"
            fi
        fi
    fi
done

echo "✅ DVC pre-commit complete."
