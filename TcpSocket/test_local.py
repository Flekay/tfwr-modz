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

            # Receive all data until connection closes or newline
            response = ""
            while True:
                chunk = s.recv(4096).decode('utf-8')
                if not chunk:
                    break
                response += chunk
                if '\n' in chunk:
                    break

            return response.strip()
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

    # Test 4: Get output
    print("\n4. Testing getoutput...")
    result = send_command("getoutput")
    print(f"   Result: {result}")

    # Test 5: Get farm
    print("\n5. Testing getfarm...")
    result = send_command("getfarm")
    print(f"   Result: {result}...")

    # Test 6: Get unlocks
    print("\n6. Testing getunlocks...")
    result = send_command("getunlocks")
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

def test_window_commands():
    print("\nTesting window commands...")

    # Test 1: Get window position
    print("\n1. Testing getwindowposition...")
    result = send_command("getwindowposition testfile")
    print(f"   Result: {result}")

    # Test 2: Move window
    print("\n2. Testing movewindow...")
    result = send_command("movewindow testfile 100 100")
    print(f"   Result: {result}")

    # Test 3: Verify position
    print("\n3. Verifying new position...")
    result = send_command("getwindowposition testfile")
    print(f"   Result: {result}")

def test_output_commands():
    print("\nTesting output commands...")

    # Test 1: Get output
    print("\n1. Testing getoutput...")
    result = send_command("getoutput")
    print(f"   Result: {result[:100]}..." if len(result) > 100 else f"   Result: {result}")

    # Test 2: Clear output
    print("\n2. Testing clearoutput...")
    result = send_command("clearoutput")
    print(f"   Result: {result}")

    # Test 3: Verify cleared
    print("\n3. Verifying output cleared...")
    result = send_command("getoutput")
    print(f"   Result: {result}")

def test_stepbystep_commands():
    print("\nTesting step-by-step execution...")

    # Create a test file with code
    print("\n1. Creating test window with code...")
    send_command("createfile steptest")
    send_command("setcode steptest for i in range(5):\n    print(i)")

    # Test 2: Start in step mode
    print("\n2. Starting execution in step-by-step mode...")
    result = send_command("stepbystep steptest")
    print(f"   Result: {result}")
    time.sleep(0.5)

    # Test 3: Advance steps
    for i in range(3):
        print(f"\n3.{i+1}. Advancing step {i+1}...")
        result = send_command("stepbystep steptest")
        print(f"   Result: {result}")
        time.sleep(0.5)

    # Clean up
    print("\n4. Cleaning up...")
    send_command("stop")
    send_command("deletefile steptest")

if __name__ == "__main__":
    # Run individual test suites
    # test_file_commands()
    # test_data_commands()
    # test_ui_control_commands()
    # test_code_commands()
    # test_game_commands()
    # test_window_commands()
    # test_output_commands()
    # test_stepbystep_commands()
    # test_exitgame()

    # Quick tests
    print("Testing TcpSocket commands...")
    result = send_command("ping")
    print(f"Ping: {result}")
