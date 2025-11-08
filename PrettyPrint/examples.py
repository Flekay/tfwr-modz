# from pprint import pprint
print = quick_print

# Example 1: Simple dictionary with Comparison
data = {15: [(1, 1), (2, 2)], 14: [(2, 2)], 13: [3]}
print("Regular print:")
print(data)

print("With pprint:")
pprint(data)


# Example 2: List of tuples
coordinates = [(1, 1), (3, 6), (4, 6), (5, 1), (6, 2), (6, 3), (6, 9), (8, 3)]
pprint(coordinates)

# Example 3: Nested lists
nested = [
    [(1, 1), (3, 6), (4, 6), (5, 1), (6, 2), (6, 3), (6, 9), (8, 3)],
    [(3, 6), (4, 6), (5, 1), (6, 2), (6, 3), (6, 9), (8, 3)],
    [(3, 6), (4, 6)]
]
pprint(nested)

# Example 4: Dictionary with tuple keys
tile_data = {
    (1, 2): "carrot",
    (3, 4): "pumpkin",
    (5, 6): "sunflower"
}
pprint(tile_data)

# Example 5: Complex nested structure
game_state = {
    "inventory": [("carrot", 10), ("pumpkin", 5)],
    "position": (3, 7),
    "unlocks": ["plant", "harvest", "water"]
}
pprint(game_state)

# Example 6: Empty collections
pprint([])
pprint({})
pprint(())

# Example 7: Sets
unique_positions = {(1, 1), (2, 2), (3, 3)}
pprint(unique_positions)

# Example 8: Long list that wraps
long_list = []
for i in range(20):
    long_list.append((i, i*2))
pprint(long_list)
