# Map string directions to GridDirection constants
DIR_MAP = {"N": North, "E": East, "S": South, "W": West}

def create_cycle(n):
	nodes = []
	dirs  = []


	# ---------- Bottom row pattern ----------
	for i in range(0, n, 2):
		nodes += [(i,1), (i,0), (i+1,0), (i+1,1)]
		if i == n-2:
			dirs += ['S','E','N','N']
		else:
			dirs += ['S','E','N','E']

	# ---------- Middle area ----------
	if n > 5:
		# 4-row slabs j = 2,6,10,... up to n-4 (exclusive of the last 3 rows)
		if n > 7:
			left_slice = len(dirs)
		# Rightward-facing “top” half of the slab (right side first):
		for i in range(n-1, 4, -2):
			nodes += [(i, 2), (i, 3), (i-1, 3), (i-1, 2)]
			dirs += ['N','W','S','W']

		# Turn around near x=2..3 to go up by one and back
		nodes += [(3, 2), (2, 2), (2, 3), (3, 3)]
		dirs += ['W','N','E','N']

		# Next two rows (shifted up two)
		nodes += [(3, 4), (2, 4), (2, 5), (3, 5)]
		dirs += ['W','N','E','E']

		# Bottom-row-style run to the right across (j+3)/(j+2)
		for i in range(4, n, 2):
			nodes += [(i, 5), (i, 4), (i+1, 4), (i+1, 5)]
			if i == n-2:
				dirs += ['S','E','N','N']
			else:
				dirs += ['S','E','N','E']

		if n > 7:
			dirs_slice_copy = dirs[left_slice:]
			nodes_slice_copy = nodes[left_slice:]
			for j in range(4, n-5, 4):
				for (x,y) in nodes_slice_copy:
					nodes.append((x,y+j))

				# use the copied dirs slice
				dirs += dirs_slice_copy

	# Optional U-shaped 2-row cap when n not divisible by 4
	if (n - 4) % 4 != 0:
		# row y = n-4: sweep leftwards then go up
		for i in range(n-1, 1, -1):
			nodes.append((i, (n-4)))
		for _ in range(n-3):
			dirs.append('W')
		dirs += ['N']

		# row y = n-3: sweep rightwards then go up
		for i in range(2, n):
			nodes.append((i, (n-3)))
		for _ in range(n-3):
			dirs.append('E')
		dirs += ['N']

	# ---------- Top row pattern ----------
	for i in range(n-1, 0, -2):
		nodes += [(i, n-2), (i, n-1), (i-1, n-1), (i-1, n-2)]
		# Your snippet had "if i == 0" but loop runs down to 1; interpret as i==1.
		if i == 1:
			dirs += ['N','W','S','S']
		else:
			dirs += ['N','W','S','W']

	# ---------- Left side vertical connector ----------
	for i in range(n-3, 2, -2):
		nodes += [(0,i), (1,i), (1,i-1), (0,i-1)]
		dirs += ['E','S','W','S']

	return nodes, dirs

def print_path(n, nodes, dirs):
	clear()
	i = 0
	for (x, y) in nodes:
		d = DIR_MAP[dirs[i]]
		arrow(x, y, d)
		i += 1

if __name__ == "__main__":
	n = get_world_size()
	if n % 2 or n < 2:
		print("n must be an even integer >= 2")
	else:
		nodes, dirs = create_cycle(n)
		print(len(nodes), "nodes;", len(dirs), "moves")
		print_path(n, nodes, dirs)
