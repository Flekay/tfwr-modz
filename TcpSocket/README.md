# TcpSocket
TCP server mod for remote game control.
## Commands
### Execution
```md
- `runstart <window>` - Start code execution
- `stop` - Stop execution
- `stepbystep <window>` - Start window in step mode or advance one step
```
### Windows
```md
- `createfile <name>` - Create window
- `deletefile <name>` - Delete window
- `setcode <name> <code>` - Set code
- `getcode <name>` - Get code
- `getwindows` - List windows (JSON)
- `movewindow <name> <x> <y>` - Move window to position
- `getwindowposition <name>` - Get window position (JSON)
```
### State
```md
- `getoutput` - Get console output
- `clearoutput` - Clear console
- `getfarm` - Get farm state (JSON)
- `getunlocks` - Get unlocks and levels (JSON)
```
### Camera
```md
- `camera <x> <y> [zoom]` - Move camera
- `camera reset` - Reset camera
```
### Game
```md
- `hideui show|hide` - Toggle UI
- `savegame` - Save game
- `loadlevel <name>` - Load level
- `getlevels` - List levels (JSON)
- `exitgame` - Exit game
- `ping` - Test connection
```
## Config
`BepInEx/config/TcpSocket.cfg`
- `Port` - TCP port (default: 9999)
- `AutoStart` - Auto-start server (default: true)
## Example Usage
```python
import socket

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
```
