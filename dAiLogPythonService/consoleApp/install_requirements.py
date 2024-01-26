# filename: install_requirements.py
import subprocess
import sys

def install(package):
    subprocess.check_call([sys.executable, "-m", "pip", "install", package])

# Install yfinance package
install("yfinance")
print("Installation completed.")