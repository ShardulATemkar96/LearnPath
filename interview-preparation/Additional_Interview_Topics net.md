# Additional Interview Topics — .NET Developer (Adform)

Supplementary topics asked alongside core .NET: Java/Collections fundamentals (interviewers
often cross-check data-structure fundamentals), queues, puzzles, LINQ/C#, and a basic React
section. Each topic follows the same format:

- **What is it?** → **Internal Working** → **Simple Example** → **When is it used?** →
  **Common Mistake** → **30-Second Interview Answer** → **Follow-Up Questions** → **Remember**

Where a topic is Java but the interview is for .NET, a **C# equivalent** line is added so you can
bridge to .NET if asked.

---

# PART 1 — JAVA HashMap + COLLECTIONS

> Note: These are Java concepts, but every one has a C# cousin. Adform is a .NET shop, so
> expect the interviewer to check your fundamentals and then ask "what is the C# equivalent?".

---

## 1. HashMap

### What is it?
A Java class that stores **key-value pairs** using a hash table. Like a phone book: give it a
name (key), get the number (value).

### Internal Working
Under the hood it keeps an array of **buckets**. To store a value, Java calls `hashCode()` on the
key, uses that to pick a bucket, and stores the entry there. To read, it does the same and returns
the value. Average time for put/get is **O(1)**.

### Simple Example
```java
HashMap<String, Integer> ages = new HashMap<>();
ages.put("Ali", 25);
ages.put("Sara", 22);
int a = ages.get("Ali");   // 25
boolean has = ages.containsKey("Sara"); // true
```

### When is it used?
- Counting occurrences (word frequency).
- Caching lookups.
- Storing config/options by name.
- Any "map one thing to another, then look it up fast" need.

### Common Mistake
- Forgetting it has **no order** — relying on insertion order fails.
- Expecting it to be thread-safe — it is not.

### 30-Second Interview Answer
"HashMap stores key-value pairs in a hash table. Put and get are O(1) on average because the key's
hashCode decides which bucket the entry lands in. I'd use it for fast lookup by key, like counting
word frequencies. It is unordered and not thread-safe."

### Follow-Up Questions
Q: Can HashMap store a null key?
A: Yes, at most one null key, but many null values.

Q: Is it thread-safe?
A: No. Use `ConcurrentHashMap` if you need thread safety.

Q: What is the C# equivalent?
A: `Dictionary<K,V>`.

### Remember
- O(1) average put/get, key-value, unordered, not thread-safe.
- C#: `Dictionary<K,V>`.

---

## 2. HashMap Internal Working

### What is it?
The step-by-step mechanism of how `put()` and `get()` find the right place.

### Internal Working
1. `put(key, value)`: call `key.hashCode()` → get an `int`.
2. A "spread" hash function mixes the bits, then the index is computed roughly as
   `hash % arrayLength`. Java uses a power-of-two array, so it uses `hash & (n - 1)` (bitwise AND,
   faster than `%`).
3. Go to that **bucket** (a slot in the array).
   - Bucket empty → create a Node there.
   - Bucket not empty → compare existing keys with `equals()`:
     - equal key found → **replace** the value.
     - not found → append to the bucket's linked list (**chaining**).
4. Java 8+: when a single bucket grows to **8+** nodes, the list converts to a **red-black tree**
   so worst case stays O(log n) instead of O(n).
5. `get(key)` mirrors put: hash the key → find bucket → walk entries comparing with `equals()`.

### Simple Example
```java
// conceptual: index = hash(key) & (capacity - 1)
map.put("Ali", 25);
// "Ali".hashCode() -> some int -> bucket index 5 -> store there
map.get("Ali"); // re-compute index 5 -> find "Ali" -> return 25
```

### When is it used?
Asked when interviewers check whether you understand hash-based structures and can analyse
worst-case behaviour.

### Common Mistake
- Claiming HashMap is **always** O(1). With a bad `hashCode`, one bucket grows and it degrades to
  O(n) (or O(log n) with the tree).
- Thinking keys are stored directly at "hashCode" position — it's `hash % size`.

### 30-Second Interview Answer
"For put, Java calls hashCode on the key and uses it to compute a bucket index in an array. Each
bucket is a linked list. If two keys collide in the same bucket they are compared with equals —
an equal key replaces the value, otherwise it is added to the list. From Java 8, long buckets turn
into trees to keep the worst case O(log n)."

### Follow-Up Questions
Q: How is the bucket index computed?
A: Roughly hashCode modulo the array length; Java sizes the array as a power of two and uses a
bitwise AND for speed.

Q: What if two keys are equal but have different hashCodes?
A: That violates the contract and breaks the map — equal objects must have equal hashCodes.

### Remember
- put = hash → index → bucket → equals. get is the same path.
- C#: `Dictionary` works the same way (hash → bucket → equality check).

---

## 3. hashCode()

### What is it?
A method on `Object` that returns an `int` — a numeric fingerprint of the object's state. It is
what places objects into buckets in hash collections.

### Internal Working
Hash collections call `hashCode()` to compute a bucket quickly instead of comparing against every
element. The contract:
- If `a.equals(b)` then `a.hashCode() == b.hashCode()` (equal objects **must** have equal hashes).
- Two different objects **may** share a hashCode (that is just a collision).

### Simple Example
```java
class Student {
    int id;
    String name;

    @Override
    public int hashCode() { return id; }   // fast, unique-ish per id

    @Override
    public boolean equals(Object o) { /* compare id AND name */ }
}
```

### When is it used?
Every hash-based collection: `HashMap`, `HashSet`, `Hashtable`.

### Common Mistake
- Overriding `equals()` without `hashCode()` — the collection puts equal objects in different
  buckets, so lookups fail.
- Returning a constant hashCode (e.g. `return 1;`) — everything lands in one bucket → O(n).
- Changing a field used by `hashCode()` after inserting the object into a map → object becomes
  un-findable.

### 30-Second Interview Answer
"hashCode gives a fast integer fingerprint of an object, and hash collections use it to decide the
bucket. The contract is that equal objects must return equal hashCodes. So whenever I override
equals, I must override hashCode too, otherwise equal objects land in different buckets and lookups
break."

### Follow-Up Questions
Q: Can two different objects have the same hashCode?
A: Yes — that is called a collision and is handled by the collection.

Q: Why both equals and hashCode, not just one?
A: hashCode finds the bucket fast; equals confirms the exact match inside the bucket.

### Remember
- equals → same hashCode (must). Same hashCode → not necessarily equal.
- C# equivalent: `GetHashCode()`.

---

## 4. equals()

### What is it?
A method from `Object` that compares the **logical equality** of two objects. The default
`Object.equals()` only compares references (memory address) — two separate objects are "not equal".

### Internal Working
After the hash finds the right bucket, `equals()` distinguishes the entries inside it. That is the
reason HashMap/HashSet need both: `hashCode()` narrows to a bucket, `equals()` picks the exact key.

### Simple Example
```java
class Student {
    int id;
    String name;

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof Student)) return false;
        Student s = (Student) o;
        return id == s.id && name.equals(s.name);
    }
    @Override
    public int hashCode() { return id; }
}
```

### When is it used?
- Looking up values in HashMap/HashSet.
- Any time two objects should be "equal" by their content, not their address.

### Common Mistake
- Using `==` on strings (`s1 == s2` compares references; use `s1.equals(s2)`).
- Overriding `equals()` without `hashCode()`.

### 30-Second Interview Answer
"equals compares logical equality — two objects with the same data are equal even if they are
different instances. Default equals only compares references. In hash collections, hashCode finds
the bucket and equals confirms the match, so they must be consistent with each other."

### Follow-Up Questions
Q: What is the difference between `==` and `equals()`?
A: `==` compares references for objects (values for primitives); `equals` compares content.

Q: What is the equals contract?
A: Reflexive, symmetric, transitive, consistent, and `equals(null)` is always false.

### Remember
- `==` = same object, `equals` = same content.
- C#: `object.Equals` / `IEquatable<T>`.

---

## 5. Hash Collisions

### What is it?
When two different keys produce the same hashCode or land in the same bucket.

### Internal Working
HashMaps handle collisions by **chaining**: the bucket holds a linked list of entries. Put appends,
get walks the list. If a bucket gets **8+** nodes, Java 8+ converts the list to a **red-black tree**
so worst-case lookup is O(log n) instead of O(n).

### Simple Example
```java
// both keys map to bucket index 3 (conceptually)
map.put("John", 30);  // bucket 3 -> ["John" -> 30]
map.put("Doe",  28);  // bucket 3 -> ["John" -> 30] -> ["Doe" -> 28]
```

### When is it used?
- Worst-case performance analysis.
- Explaining why a good `hashCode` matters.

### Common Mistake
- Thinking collisions are impossible.
- Thinking same hashCode means same object — it just means same bucket.

### 30-Second Interview Answer
"A collision is when two keys land in the same bucket. HashMap handles it with chaining — a linked
list per bucket — and from Java 8 it converts long lists into red-black trees so the worst case stays
O(log n). A good hashCode spreads keys evenly and keeps collisions rare."

### Follow-Up Questions
Q: How is a collision detected?
A: By comparing keys with `equals()` inside the same bucket.

Q: What makes collisions worse?
A: A poor `hashCode` (e.g. returning a constant).

### Remember
- Collision = same bucket. Chaining solves it; trees rescue the worst case.

---

## 6. Buckets

### What is it?
Buckets are the slots of the underlying array that HashMap is built on. Each bucket holds zero, one,
or several entries.

### Internal Working
The array starts at capacity **16** (a power of two). Index for a key ≈ `hash & (n - 1)`. A bucket
with multiple entries is a linked list (or tree from 8+). Entry count = number of keys stored, not
buckets.

### Simple Example
```java
// capacity 16 -> valid bucket indices 0..15
map.put("Ali", 25); // lands in some bucket, say index 5
```

### When is it used?
When explaining internal working or memory usage of hash maps.

### Common Mistake
- Thinking one bucket can only hold one key.
- Confusing capacity (number of buckets) with size (number of entries).

### 30-Second Interview Answer
"Buckets are the slots of the array inside a HashMap. Each bucket is either empty or holds a linked
list of entries. The hash of the key decides which bucket to use, and if several keys collide they
just share the same bucket."

### Follow-Up Questions
Q: How many buckets does a new HashMap have?
A: 16 by default.

Q: What is capacity vs size?
A: Capacity = number of buckets; size = number of stored key-value pairs.

### Remember
- Bucket = array slot, may hold many entries. C#: same idea in `Dictionary`'s internal buckets array.

---

## 7. Load Factor

### What is it?
A threshold ratio that decides **when** the HashMap grows. Default is **0.75**.

### Internal Working
HashMap resizes when `size > capacity * loadFactor`. Default: `16 * 0.75 = 12`, so the 13th insert
triggers a resize. This balances **memory usage** against **collisions**: higher load factor uses less
memory but more collisions; lower uses more memory but fewer collisions.

### Simple Example
```java
HashMap<String, Integer> map = new HashMap<>(); // capacity 16, load factor 0.75
// after 12 entries stored, the 13th insert triggers a resize to 32
```

### When is it used?
- Choosing an initial capacity to avoid repeated resizing.
- Explaining HashMap performance/memory trade-off.

### Common Mistake
- Setting a huge initial capacity "just in case" and wasting memory.
- Forgetting the resize cost when inserting many elements.

### 30-Second Interview Answer
"Load factor is the ratio of stored entries to capacity that triggers a resize. The default 0.75
means the map doubles its size when it is 75% full, balancing memory usage against collision risk.
If I know the map will be large, I set a good initial capacity to avoid resizes."

### Follow-Up Questions
Q: What is the default load factor?
A: 0.75.

Q: What happens at load factor?
A: The array doubles in size and every existing key is rehashed.

### Remember
- Size > capacity × 0.75 → resize. C#: `Dictionary` uses its own resizing on the same principle.

---

## 8. Resizing / Rehashing

### What is it?
Growing the HashMap's array and re-computing positions for all stored keys.

### Internal Working
When the load factor is exceeded:
1. Create a new array of **double** the capacity (16 → 32 → 64 …).
2. For every existing key, recompute the bucket index (`hash & newSize - 1`) — indices change.
3. Move the entries (rehash) into the new array.

Cost is **O(n)** for one resize, but because it happens rarely, **amortized** put stays O(1).

### Simple Example
```java
// capacity 16, 13th insert triggers:
// new capacity 32, every key's bucket index is re-computed
```

### When is it used?
- Explaining amortized O(1) insertion.
- Why setting initial capacity helps performance.

### Common Mistake
- Not knowing indices change after resize (so keys are re-located).
- Storing a **mutable** key whose hashCode changes — it gets lost in the wrong bucket after resize.

### 30-Second Interview Answer
"When the load factor is crossed, HashMap doubles its array and re-computes the bucket for every
existing key — that is rehashing. One resize costs O(n), but it happens rarely, so inserts stay O(1)
amortized. To avoid many resizes I can set a sensible initial capacity up front."

### Follow-Up Questions
Q: Is HashMap insertion really O(1)?
A: Amortized O(1); a single resize is O(n) but is infrequent.

Q: Why use power-of-two capacity?
A: So `index = hash & (n - 1)` replaces the slower modulo.

### Remember
- Resize = new array ×2 + rehash all keys. Amortized O(1) put.

---

## 9. Collection Framework

### What is it?
A unified set of interfaces (List, Set, Queue, Map) and their implementations (ArrayList, HashSet,
TreeMap, …) that Java provides for storing and manipulating groups of objects.

### Internal Working
The hierarchy:
```
Iterable
 └── Collection
      ├── List  (ordered, allows duplicates)
      ├── Set   (no duplicates)
      └── Queue (FIFO processing)
Map (separate interface: key-value pairs)
```
`Collections` is NOT part of this — it is a separate utility class.

### Simple Example
```java
List<String> list = new ArrayList<>();  // interface + implementation
Set<Integer> set = new HashSet<>();
Map<String, Integer> map = new HashMap<>();
```

### When is it used?
Every time you store data in Java: lists for ordered data, sets for uniqueness, maps for lookups.

### Common Mistake
- Confusing `Collection` (the interface) with `Collections` (the utility class).
- Using a concrete type everywhere instead of the interface (reduces flexibility).

### 30-Second Interview Answer
"The Collection Framework is Java's set of interfaces and implementations for storing groups of
objects — List, Set, and Queue under the Collection interface, plus Map for key-value pairs.
Programming to the interface lets me swap implementations easily."

### Follow-Up Questions
Q: Is Map part of Collection?
A: No — it is part of the framework but a separate interface.

Q: What is the C# equivalent?
A: `System.Collections.Generic` (IList, ISet, IDictionary, ICollection).

### Remember
- Collection = interface. Collections = utility class. Map is separate.
- C#: `System.Collections.Generic`.

---

## 10. List

### What is it?
An **ordered** collection that **allows duplicate** elements and gives access by **index**.

### Internal Working
`List` is an interface; its two main implementations are `ArrayList` (backed by an array) and
`LinkedList` (doubly linked nodes). Order is the insertion order.

### Simple Example
```java
List<String> names = new ArrayList<>();
names.add("Ali");   // index 0
names.add("Sara");  // index 1
String first = names.get(0); // "Ali"
```

### When is it used?
- When order matters and duplicates are fine: to-do items, results, history, rows of data.

### Common Mistake
- Using a List when you need uniqueness (use a Set).
- Using `get(i)` in a loop on a LinkedList (O(n) each access → O(n²)).

### 30-Second Interview Answer
"List is an ordered collection that allows duplicates and supports index access. I use it whenever
the order of items matters, like a list of results. Its two common implementations are ArrayList
and LinkedList."

### Follow-Up Questions
Q: Can a List contain null?
A: Yes, implementations like ArrayList allow nulls.

Q: C# equivalent?
A: `IList<T>` / `List<T>`.

### Remember
- Ordered + duplicates + index. C#: `List<T>`.

---

## 11. ArrayList

### What is it?
A resizable array implementation of `List`. The most commonly used list.

### Internal Working
Backed by a plain array. When full, it grows by **~50%** (new array, copy elements — O(n)). Random
access `get(i)` is **O(1)**; inserting/deleting in the middle is **O(n)** because elements shift.

### Simple Example
```java
ArrayList<String> list = new ArrayList<>();
list.add("A"); list.add("B"); list.add("C");
String b = list.get(1);  // O(1)
list.add(1, "X");        // O(n) - shifts B and C
```

### When is it used?
- Default choice when you need indexed access and order.
- Reading/writing data mostly at the end.

### Common Mistake
- Using `remove(0)` on a large ArrayList in a loop (O(n) each time → O(n²)).
- Inserting at the front repeatedly.

### 30-Second Interview Answer
"ArrayList is a List backed by a dynamic array. Getting by index is O(1), but inserting or deleting
in the middle is O(n) because elements shift. It grows automatically when full, so it's my default
list choice."

### Follow-Up Questions
Q: What is its growth factor?
A: About 1.5× when full.

Q: C# equivalent?
A: `List<T>` (same resizable-array idea).

### Remember
- Random access fast, middle insert/delete slow. C#: `List<T>`.

---

## 12. LinkedList

### What is it?
A `List` (and `Deque`) implementation made of linked nodes: each node holds data + a pointer to the
next (and previous) node.

### Internal Working
No array — nodes point to each other. Adding/removing at the **ends** is **O(1)** (just re-point).
Access by index is **O(n)** — you walk from the head. It implements both `List` and `Deque`.

### Simple Example
```java
LinkedList<String> list = new LinkedList<>();
list.addFirst("first");   // O(1)
list.addLast("last");     // O(1)
String s = list.get(1);   // O(n) - walks nodes
```

### When is it used?
- Frequent insert/remove at both ends (like a queue or stack).
- When random access is rare.

### Common Mistake
- Using it for random-access loops (`get(i)`) — O(n) per call.
- Forgetting each node costs extra memory for the pointers.

### 30-Second Interview Answer
"LinkedList stores elements as nodes that point to each other. Adding or removing at the ends is
O(1), but accessing by index is O(n) because you walk the chain. I'd choose it when I mostly add
and remove at both ends."

### Follow-Up Questions
Q: ArrayList vs LinkedList for get(i)?
A: ArrayList O(1), LinkedList O(n).

Q: C# equivalent?
A: `LinkedList<T>`.

### Remember
- Ends fast (O(1)), index slow (O(n)). C#: `LinkedList<T>`.

---

## 13. Set

### What is it?
A `Collection` that stores **unique** elements — no duplicates.

### Internal Working
Duplicate detection relies on `equals()`/`hashCode()` (in `HashSet`) or ordering (`TreeSet`).
Adding a duplicate simply does nothing.

### Simple Example
```java
Set<String> set = new HashSet<>();
set.add("A"); set.add("A"); set.add("B");
// set contains only "A" and "B"
```

### When is it used?
- Removing duplicates.
- Membership checks ("does this exist?").

### Common Mistake
- Assuming a Set keeps insertion order (only `LinkedHashSet` does).
- Using a List just to avoid duplicate logic.

### 30-Second Interview Answer
"A Set is a collection that guarantees uniqueness — adding a duplicate is ignored. I use it when I
need to remove duplicates or check membership. HashSet gives O(1) operations; TreeSet keeps elements
sorted."

### Follow-Up Questions
Q: Can a Set contain null?
A: HashSet allows one null; TreeSet does not.

Q: C# equivalent?
A: `ISet<T>` / `HashSet<T>`.

### Remember
- Unique elements. HashSet = O(1), TreeSet = sorted.

---

## 14. HashSet

### What is it?
The most common `Set` implementation — backed by a **HashMap** (values are a constant dummy object).

### Internal Working
It is literally a `HashMap<E, Object>` where every value is a shared dummy. `add(x)` does
`map.put(x, PRESENT)`; `contains(x)` does `map.containsKey(x)`. So O(1) average for add/contains,
**unordered**, and it allows one `null`.

### Simple Example
```java
HashSet<String> set = new HashSet<>();
set.add("apple"); set.add("banana");
boolean has = set.contains("apple"); // true, O(1)
```

### When is it used?
- Fast uniqueness and membership checks.
- Deduplicating data before further processing.

### Common Mistake
- Expecting order — HashSet is unordered; use `LinkedHashSet` for insertion order.
- Forgetting elements must have proper `equals`/`hashCode`.

### 30-Second Interview Answer
"HashSet is a Set backed by a HashMap — the elements are the keys with a shared dummy value. That
gives O(1) average add and contains, but no guaranteed order. I use it for fast uniqueness and
membership checks."

### Follow-Up Questions
Q: How is HashSet backed by HashMap?
A: Elements are stored as map keys; values are a shared constant.

Q: C# equivalent?
A: `HashSet<T>`.

### Remember
- HashMap keys under the hood → O(1), unordered, one null.

---

## 15. TreeSet

### What is it?
A `Set` implementation that keeps elements in **sorted order**, backed by a red-black tree.

### Internal Working
Elements are stored in a **balanced binary search tree** (red-black). Add/remove/contains are
**O(log n)**. Order comes from `Comparable` (natural order) or a `Comparator` you supply. `null` is
not allowed.

### Simple Example
```java
TreeSet<Integer> set = new TreeSet<>();
set.add(5); set.add(1); set.add(3);
// iteration order: 1, 3, 5  (sorted)
int first = set.first(); // 1
```

### When is it used?
- When you need sorted, unique elements.
- Range queries (`headSet`, `tailSet`, `subSet`).

### Common Mistake
- Using TreeSet when you only need uniqueness — HashSet is faster (O(1) vs O(log n)).
- Forgetting elements must be `Comparable` or you must pass a `Comparator`.

### 30-Second Interview Answer
"TreeSet is a Set backed by a red-black tree, so elements are kept sorted and operations are O(log n).
I use it when I need sorted unique data or range lookups. If I only need uniqueness, HashSet is
faster."

### Follow-Up Questions
Q: How does TreeSet know the order?
A: From the element's Comparable, or a Comparator you provide.

Q: C# equivalent?
A: `SortedSet<T>`.

### Remember
- Sorted + unique, O(log n). C#: `SortedSet<T>`.

---

## 16. Map

### What is it?
An interface for **key-value** pairs where each key maps to exactly one value. Keys are unique.

### Internal Working
Not part of the `Collection` interface but part of the framework. Common implementations:
`HashMap` (hash table, O(1)), `TreeMap` (sorted tree, O(log n)), `LinkedHashMap` (insertion order).

### Simple Example
```java
Map<String, Integer> map = new HashMap<>();
map.put("Ali", 25);
Integer age = map.get("Ali"); // 25
```

### When is it used?
- Lookups by key: user id → profile, product name → price.
- Caching, counting, grouping.

### Common Mistake
- Assuming iteration order (depends on implementation).
- Using `map.get(key)` and treating null as "value is null" vs "key missing" (use `containsKey`).

### 30-Second Interview Answer
"Map stores key-value pairs with unique keys. The interface is separate from Collection, and its
implementations trade speed and order — HashMap is O(1), TreeMap keeps keys sorted. I use it any
time I look things up by a key."

### Follow-Up Questions
Q: Can a Map have duplicate keys?
A: No — a later put with the same key overwrites the value.

Q: C# equivalent?
A: `IDictionary<K,V>` / `Dictionary<K,V>`.

### Remember
- Unique keys, one value per key. C#: `Dictionary<K,V>`.

---

## 17. TreeMap

### What is it?
A `Map` implementation backed by a **red-black tree** that keeps keys in sorted order.

### Internal Working
Keys are sorted (natural order or via `Comparator`). Put/get/remove are **O(log n)**. `null` keys
are not allowed (null values are). Supports range queries like `subMap`, `headMap`.

### Simple Example
```java
TreeMap<String, Integer> map = new TreeMap<>();
map.put("b", 2); map.put("a", 1); map.put("c", 3);
// keys iterate as a, b, c
String firstKey = map.firstKey(); // "a"
```

### When is it used?
- When keys must be in sorted order.
- Range lookups on keys.

### Common Mistake
- Using TreeMap when you only need fast lookup — HashMap is faster (O(1) vs O(log n)).
- Putting a null key — throws `NullPointerException`.

### 30-Second Interview Answer
"TreeMap is a Map backed by a red-black tree, so keys are always sorted and operations are O(log n).
I use it when I need ordered keys or range queries. For plain fast lookups HashMap is the better
choice."

### Follow-Up Questions
Q: What is the difference between TreeMap and HashMap order?
A: TreeMap keeps keys sorted; HashMap has no order.

Q: C# equivalent?
A: `SortedDictionary<K,V>`.

### Remember
- Sorted keys, O(log n), no null key. C#: `SortedDictionary<K,V>`.

---

## 18. Key Comparisons

### ArrayList vs LinkedList

| | ArrayList | LinkedList |
|---|---|---|
| Backed by | Dynamic array | Linked nodes |
| get(i) | **O(1)** | O(n) |
| Add at end | O(1) amortized | O(1) |
| Insert/remove middle | O(n) (shifts) | O(n) (find, then re-point) |
| Add/remove at ends | O(n) at front | **O(1)** |
| Memory | Less per element | Extra per node (pointers) |
| C# equivalent | `List<T>` | `LinkedList<T>` |

**Interview line:** "Use ArrayList for index access and end-appends; use LinkedList when most work
is at the ends. ArrayList is the usual default."

### HashSet vs TreeSet

| | HashSet | TreeSet |
|---|---|---|
| Underlying | HashMap | Red-black tree |
| Order | None | Sorted |
| Add/contains | **O(1)** average | O(log n) |
| Allows null | Yes (one) | No |
| C# equivalent | `HashSet<T>` | `SortedSet<T>` |

**Interview line:** "HashSet is faster and unordered; TreeSet keeps elements sorted but O(log n).
Choose based on whether you need order."

### HashMap vs TreeMap

| | HashMap | TreeMap |
|---|---|---|
| Underlying | Hash table | Red-black tree |
| Key order | None | Sorted |
| Put/get | **O(1)** average | O(log n) |
| Null key | Allowed (one) | Not allowed |
| Range queries | No | Yes |
| C# equivalent | `Dictionary<K,V>` | `SortedDictionary<K,V>` |

**Interview line:** "HashMap for speed, TreeMap for sorted keys and range queries."

### List vs Set

| | List | Set |
|---|---|---|
| Duplicates | Allowed | Not allowed |
| Order | Insertion/index | Depends (HashSet unordered, TreeSet sorted) |
| Access | By index | By membership |
| Typical impl | ArrayList | HashSet |

**Interview line:** "List when order matters and duplicates are fine; Set when uniqueness matters."

### Collection vs Collections

| | Collection | Collections |
|---|---|---|
| Type | Interface | Utility class |
| Contains | List, Set, Queue | Static methods |
| Example | `Collection<String> c` | `Collections.sort(list)`, `Collections.reverse`, `Collections.unmodifiableList` |

**Interview line:** "Collection is the root interface of the framework; Collections is a helper class
full of static methods like sort and reverse. They're easy to confuse by name only."

---

*End of Part 1 (Phase 1). Continue with Phase 2 (Queue + PriorityQueue) when prompted.*
