#!/bin/sh
set -e

echo "🚀 ProjectX Onboarding Script (OAuth 버전)"

# 1) Git hooks path는 Husky가 자동 관리하므로 core.hooksPath 변경 불필요
echo "✅ Husky는 .husky 디렉토리를 자동 사용합니다"

# ------------------------------------------------------------
# 0) Python 확인
# ------------------------------------------------------------
if command -v python3 >/dev/null 2>&1; then
    echo "✅ Python3 is installed: $(python3 --version)"
else
    echo "❌ Python3 not found. Installing..."
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        sudo apt-get update && sudo apt-get install -y python3 python3-pip
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        brew install python
    else
        echo "⚠️ Unsupported OS for auto-install. Please install Python manually."
        exit 1
    fi
fi

# ------------------------------------------------------------
# 1) Node.js & npm 확인
# ------------------------------------------------------------
if command -v node >/dev/null 2>&1; then
    echo "✅ Node.js is installed: $(node --version)"
else
    echo "❌ Node.js not found. Installing..."
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
        sudo apt-get install -y nodejs
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        brew install node
    else
        echo "⚠️ Unsupported OS for auto-install. Please install Node.js manually."
        exit 1
    fi
fi

if command -v npm >/dev/null 2>&1; then
    echo "✅ npm is installed: $(npm --version)"
else
    echo "❌ npm is missing. Check Node.js installation."
    exit 1
fi

# 2) Husky 설치
if [ -d ".husky" ]; then
    echo "🔧 Installing Husky..."
    npm install
    npm run prepare
    echo "✅ Husky installed successfully"
else
    echo "⚠️ .husky directory not found, skipping Husky setup."
fi

# ------------------------------------------------------------
# 3) Husky hooks 권한 부여 + Git hook 경로 설정
# ------------------------------------------------------------
if [ -d ".husky" ]; then
    echo "🔧 Setting execute permission for Husky hooks..."
    chmod +x .husky/*

    echo "🔧 Configuring Git to use Husky hooks..."
    git config core.hooksPath .husky

    echo "✅ Husky hooks are now active"
else
    echo "⚠️ .husky directory not found, skipping Husky hook setup."
fi


# ------------------------------------------------------------
# DVC 설치 확인
# ------------------------------------------------------------
if command -v dvc >/dev/null 2>&1; then
    echo "✅ DVC is already installed: $(dvc --version)"
else
    echo "🔧 Installing DVC..."
    pip3 install --upgrade pip
    pip3 install "dvc[gdrive]" --upgrade
    if command -v dvc >/dev/null 2>&1; then
        echo "✅ DVC installed successfully: $(dvc --version)"
    else
        echo "❌ DVC installation failed. Please check PATH or Python environment."
        exit 1
    fi
fi

# 2) DVC 초기화 (이미 되어 있으면 스킵)
if [ ! -d ".dvc" ]; then
    echo "🔧 Initializing DVC..."
    dvc init
    git add .dvc .gitignore
    git commit -m "chore: init DVC" || true
else
    echo "✅ DVC already initialized"
fi

# 3) DVC 원격(remote) 설정
REMOTE_NAME="myremote"
REMOTE_URL="gdrive://1hdp9DIUfbvA_IaBiJOVHB7zPZaOiGyp1"

if ! dvc remote list | grep -q "$REMOTE_NAME"; then
    echo "🔧 Setting up DVC remote..."
    dvc remote add -d $REMOTE_NAME $REMOTE_URL
else
    echo "✅ DVC remote already exists"
fi

# 4) Google Drive OAuth 클라이언트 설정 (로컬에만 저장)
export DVC_GDRIVE_CLIENT_ID="340300789745-vrq2fml3hcv9mr4i72u2r3r84ciik39k.apps.googleusercontent.com"
export DVC_GDRIVE_CLIENT_SECRET="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"

dvc remote modify --local $REMOTE_NAME gdrive_client_id "$DVC_GDRIVE_CLIENT_ID"
dvc remote modify --local $REMOTE_NAME gdrive_client_secret "$DVC_GDRIVE_CLIENT_SECRET"
echo "✅ DVC Google Drive OAuth configured (local only)"

# 5) 최초 데이터 pull (브라우저 인증 필요)
echo "📥 Running 'dvc pull' (login in browser if prompted)..."
dvc pull || true




echo "✅ setup-hooks.sh completed successfully!"


echo "🎉 Onboarding complete!"
