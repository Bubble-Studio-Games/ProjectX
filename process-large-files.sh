#!/bin/bash
LIMIT=104857600   # 100MB
failed_files=()

# HEAD 없는 초기 repo 대응 → 모든 파일 (untracked + staged) 검사
git ls-files --others --cached --exclude-standard -z | while IFS= read -r -d '' file; do
    if [ -f "$file" ]; then
        ext="${file##*.}"
        size=$(wc -c <"$file")
        process=false

        # 확장자 규칙
        case "$ext" in
            fbx|anim|wav|mp4|psd|exr|hdr|tga)  # 무조건 DVC
                process=true ;;
            png|jpg|obj|blend|prefab)          # 조건부: 100MB 초과
                [ $size -gt $LIMIT ] && process=true ;;
        esac

        if [ "$process" = true ]; then
            echo "⚠️ Processing file for DVC: $file (size: $size bytes)"

            # 이미 DVC 추적 중인지 확인 (.dvc 메타파일 존재 여부)
            if [ -f "$file.dvc" ]; then
                echo "ℹ️ Already tracked by DVC (.dvc exists): $file → skip"
                continue
            fi

            if dvc add "$file"; then
                echo "✅ dvc add succeeded for $file"

                # Git에 올라간 파일이면 캐시에서 제거
                if git ls-files --cached --error-unmatch "$file" >/dev/null 2>&1; then
                    git rm --cached "$file"
                fi

                # .dvc 파일 및 .gitignore 자동 추적
                [ -f "$file.dvc" ] && git add "$file.dvc"
                dir=$(dirname "$file")
                [ -f "$dir/.gitignore" ] && git add "$dir/.gitignore"
            else
                echo "❌ dvc add failed for $file"
                failed_files+=("$file")
            fi
        fi
    fi
done

if [ ${#failed_files[@]} -gt 0 ]; then
    echo ""
    echo "❌ Failed to process the following files:"
    for f in "${failed_files[@]}"; do
        echo "   - $f"
    done
    exit 1
else
    echo ""
    echo "✅ All target files processed successfully."
fi
