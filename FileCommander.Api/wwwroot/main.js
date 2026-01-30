(() => {
    const apiBase = "/api/filesystem";

    const panels = {
        left: createPanel("left"),
        right: createPanel("right")
    };

    let activePanel = "left";

    document.querySelectorAll(".panel").forEach(p => {
        p.addEventListener("click", () => {
            setActivePanel(p.dataset.panel);
        });
    });

    document.getElementById("btn-copy").addEventListener("click", () => handleOperation("copy"));
    document.getElementById("btn-move").addEventListener("click", () => handleOperation("move"));
    document.getElementById("btn-delete").addEventListener("click", () => handleOperation("delete"));

    document.addEventListener("keydown", (e) => {
        if (e.key === "F5") {
            e.preventDefault();
            handleOperation("copy");
        } else if (e.key === "F6") {
            e.preventDefault();
            handleOperation("move");
        } else if (e.key === "F8") {
            e.preventDefault();
            handleOperation("delete");
        }
    });

    loadDrives().then(drives => {
        Object.values(panels).forEach(panel => {
            initPanel(panel, drives);
        });
    }).catch(err => setStatus("Ошибка загрузки дисков: " + err.message));

    function createPanel(key) {
        const root = document.querySelector(`.panel[data-panel="${key}"]`);
        return {
            key,
            root,
            driveSelect: root.querySelector(".drive-select"),
            pathInput: root.querySelector(".path-input"),
            upButton: root.querySelector(".up-button"),
            tbody: root.querySelector(".entries-body"),
            state: {
                currentPath: "",
                entries: [],
                selectedPaths: new Set()
            }
        };
    }

    function setActivePanel(key) {
        activePanel = key;
        document.querySelectorAll(".panel").forEach(p => p.classList.remove("active"));
        panels[key].root.classList.add("active");
    }

    function initPanel(panel, drives) {
        panel.driveSelect.innerHTML = "";
        drives.forEach(d => {
            const opt = document.createElement("option");
            opt.value = d;
            opt.textContent = d;
            panel.driveSelect.appendChild(opt);
        });

        panel.driveSelect.addEventListener("change", () => {
            const drive = panel.driveSelect.value;
            if (drive) {
                loadEntries(panel, drive);
            }
        });

        panel.upButton.addEventListener("click", () => {
            if (!panel.state.currentPath) return;
            const parent = getParentDirectory(panel.state.currentPath);
            if (parent) {
                loadEntries(panel, parent);
            }
        });

        if (drives.length > 0) {
            panel.driveSelect.value = drives[0];
            loadEntries(panel, drives[0]);
        }
    }

    async function loadDrives() {
        const res = await fetch(`${apiBase}/drives`);
        if (!res.ok) {
            throw new Error("HTTP " + res.status);
        }
        return await res.json();
    }

    async function loadEntries(panel, path) {
        setStatus("Загрузка " + path + " ...");
        panel.state.selectedPaths.clear();

        const res = await fetch(`${apiBase}/entries?path=${encodeURIComponent(path)}`);
        if (!res.ok) {
            setStatus("Ошибка: " + res.statusText);
            return;
        }

        const data = await res.json();
        panel.state.currentPath = path;
        panel.state.entries = data;
        panel.pathInput.value = path;
        renderEntries(panel);

        if (!document.querySelector(".panel.active")) {
            setActivePanel(panel.key);
        }

        setStatus("");
    }

    function renderEntries(panel) {
        panel.tbody.innerHTML = "";

        panel.state.entries.forEach(entry => {
            const tr = document.createElement("tr");
            tr.className = "entry-row";

            const isSelected = panel.state.selectedPaths.has(entry.fullPath);
            if (isSelected) {
                tr.classList.add("selected");
            }

            tr.addEventListener("click", (e) => {
                if (e.detail === 2) {
                    if (entry.isDirectory) {
                        loadEntries(panel, entry.fullPath);
                    }
                    return;
                }

                toggleSelection(panel, entry.fullPath, tr);
            });

            const tdName = document.createElement("td");
            tdName.className = "col-name";
            const nameDiv = document.createElement("div");
            nameDiv.className = "entry-name";

            const iconSpan = document.createElement("span");
            iconSpan.className = "entry-icon " + iconClass(entry.iconKey, entry.isDirectory);

            const textSpan = document.createElement("span");
            textSpan.textContent = entry.name;

            nameDiv.appendChild(iconSpan);
            nameDiv.appendChild(textSpan);
            tdName.appendChild(nameDiv);

            const tdSize = document.createElement("td");
            tdSize.className = "col-size";
            tdSize.textContent = entry.formattedSize || "";

            const tdDate = document.createElement("td");
            tdDate.className = "col-date";
            tdDate.textContent = formatDate(entry.lastModified);

            tr.appendChild(tdName);
            tr.appendChild(tdSize);
            tr.appendChild(tdDate);

            panel.tbody.appendChild(tr);
        });
    }

    function toggleSelection(panel, fullPath, rowEl) {
        if (panel.state.selectedPaths.has(fullPath)) {
            panel.state.selectedPaths.delete(fullPath);
            rowEl.classList.remove("selected");
        } else {
            panel.state.selectedPaths.add(fullPath);
            rowEl.classList.add("selected");
        }
    }

    function iconClass(iconKey, isDirectory) {
        if (isDirectory) return "folder";
        switch (iconKey) {
            case "file-text": return "file-text";
            case "file-image": return "file-image";
            case "file-audio": return "file-audio";
            case "file-video": return "file-video";
            case "file-binary": return "file-binary";
            default: return "file";
        }
    }

    function formatDate(iso) {
        if (!iso) return "";
        const d = new Date(iso);
        if (isNaN(d.getTime())) return "";

        const pad = (n) => n.toString().padStart(2, "0");

        const day = pad(d.getDate());
        const month = pad(d.getMonth() + 1);
        const year = d.getFullYear();
        const hours = pad(d.getHours());
        const minutes = pad(d.getMinutes());
        const seconds = pad(d.getSeconds());

        return `${day}.${month}.${year} ${hours}:${minutes}:${seconds}`;
    }

    function getParentDirectory(path) {
        if (!path) return null;
        const trimmed = path.replace(/[/\\]+$/, "");
        const idx = trimmed.lastIndexOf("\\");
        if (idx <= 2) {
            const drive = trimmed.substring(0, 3);
            return drive;
        }
        return trimmed.substring(0, idx);
    }

    async function handleOperation(kind) {
        const from = panels[activePanel];
        const to = panels[activePanel === "left" ? "right" : "left"];

        const selected = Array.from(from.state.selectedPaths);

        if (selected.length === 0) {
            setStatus("Не выбрано ни одного элемента.");
            return;
        }

        if (kind !== "delete" && !to.state.currentPath) {
            setStatus("Не задана целевая директория.");
            return;
        }

        if (kind === "delete" && !confirm(`Удалить ${selected.length} элемент(ов)?`)) {
            return;
        }

        try {
            setStatus("Выполнение операции...");

            if (kind === "copy" || kind === "move") {
                const body = {
                    sourcePaths: selected,
                    destinationDirectory: to.state.currentPath
                };

                const res = await fetch(`${apiBase}/${kind}`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(body)
                });
                if (!res.ok) {
                    throw new Error(res.statusText);
                }

                await loadEntries(from, from.state.currentPath);
                await loadEntries(to, to.state.currentPath);
            } else if (kind === "delete") {
                const res = await fetch(`${apiBase}/delete`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(selected)
                });
                if (!res.ok) {
                    throw new Error(res.statusText);
                }

                await loadEntries(from, from.state.currentPath);
            }

            setStatus("Готово.");
            setTimeout(() => setStatus(""), 2000);
        } catch (err) {
            console.error(err);
            setStatus("Ошибка операции: " + err.message);
        }
    }

    function setStatus(text) {
        const el = document.getElementById("status");
        if (el) {
            el.textContent = text || "";
        }
    }
})();

