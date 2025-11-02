import socket
import time
import json
import random

def send_command(command, ip='127.0.0.1', port=9999):
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.settimeout(5)
            s.connect((ip, port))
            s.sendall(f"{command}\n".encode('utf-8'))
            response = s.recv(1024).decode('utf-8').strip()
            return response
    except Exception as e:
        return f"ERROR: {str(e)}"

def test_file_commands():
    print("\nTesting file-related commands...")

    # Test 1: Create file
    print("\n1. Testing createfile...")
    result = send_command("createfile test_window")
    print(f"   Result: {result}")

    # Test 2: Set code
    print("\n2. Testing setcode...")
    result = send_command("setcode test_window print('Hello from API!')")
    print(f"   Result: {result}")

    # Test 3: Get code
    print("\n3. Testing getcode...")
    result = send_command("getcode test_window")
    print(f"   Result: {result}")

    # Test 4: Delete file
    print("\n4. Testing deletefile...")
    result = send_command("deletefile test_window")
    print(f"   Result: {result}")

def test_data_commands():
    print("\nTesting data-related commands...")

    # Test 1: Ping
    print("\n1. Testing ping...")
    result = send_command("ping")
    print(f"   Result: {result}")

    # Test 2: Get levels
    print("\n2. Testing getlevels...")
    result = send_command("getlevels")
    print(f"   Result: {result}")

    # Test 3: Get windows
    print("\n3. Testing getwindows...")
    result = send_command("getwindows")
    print(f"   Result: {result}")


def test_ui_control_commands():
    print("\nTesting UI control commands...")

    # Test: Hide/Show UI
    print("\n1. Testing hideui...")
    result = send_command("hideui true")
    print(f"   Result: {result}")
    time.sleep(1)

    result = send_command("hideui false")
    print(f"   Result: {result}")

    # Test: Camera control
    print("\n2. Testing camera...")
    result = send_command("camera 10 5 1.5")
    print(f"   Result: {result}")
    time.sleep(0.5)

    result = send_command("camera reset")
    print(f"   Result: {result}")

def test_code_commands():
    print("\nTesting code execution commands...")

    # Test: Run window
    print("\n1. Testing runstart...")
    send_command("createfile test_window")
    send_command("setcode test_window " \
    "while True:\n    print('Running from test_window')")
    result = send_command("runstart test_window")
    print(f"   Result: {result}")
    time.sleep(1)

    # Test: Stop execution
    print("\n2. Testing stop...")
    result = send_command("stop")
    print(f"   Result: {result}")

def test_game_commands():
    print("\nTesting game state commands...")

    # Test 1: Save game
    print("\n1. Testing savegame...")
    result = send_command("savegame")
    print(f"   Result: {result}")

    # Test 2: Load level
    print("\n2. Testing loadlevel...")
    result = send_command("getlevels")
    print(f"   Available levels: {result}")

    # Parse the JSON array and load a random level
    try:
        levels = json.loads(result)
        if levels and isinstance(levels, list) and len(levels) > 0:
            random_level = random.choice(levels)
            print(f"\n   Loading random level: {random_level}")
            load_result = send_command(f"loadlevel {random_level}")
            print(f"   Load result: {load_result}")
        else:
            print("   No levels available to load")
    except json.JSONDecodeError:
        print(f"   Could not parse levels: {result}")


def test_exitgame():
    """Test exit game command."""
    print("\nTesting exitgame command...")
    result = send_command("exitgame")
    print(f"   Result: {result}")

if __name__ == "__main__":
    # Run individual test suites
    test_file_commands()
    test_data_commands()
    test_ui_control_commands()
    test_code_commands()
    test_game_commands()
    test_exitgame()
