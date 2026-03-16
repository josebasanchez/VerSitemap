(function () {
  const domainInput = document.getElementById("domainInput");
  const loadBtn = document.getElementById("loadBtn");
  const generateBtn = document.getElementById("generateBtn");
  const urlList = document.getElementById("urlList");
  const domainLabel = document.getElementById("domainLabel");
  const progressLabel = document.getElementById("progressLabel");

  if (!domainInput || !loadBtn || !generateBtn || !urlList || !domainLabel || !progressLabel) {
    return;
  }

  function statusClass(status) {
    switch (status) {
      case 2:
        return "status-ok";
      case 3:
        return "status-error";
      case 1:
        return "status-checking";
      default:
        return "";
    }
  }

  function renderItem(item) {
    const li = document.createElement("li");
    li.className = ["url-item", statusClass(item.status)].filter(Boolean).join(" ");
    li.dataset.url = item.url;

    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.checked = item.selected;
    checkbox.dataset.url = item.url;

    const link = document.createElement("a");
    link.href = item.url;
    link.textContent = item.url;
    link.target = "_blank";
    link.rel = "noreferrer";

    const chip = document.createElement("span");
    chip.className = "status-chip";
    chip.textContent = item.statusDetail ? `(${item.statusDetail})` : "";

    li.appendChild(checkbox);
    li.appendChild(link);
    li.appendChild(chip);
    return li;
  }

  function renderState(state) {
    domainLabel.textContent = `Dominio: ${state.domain || "-"}`;
    if (state.domain) {
      domainInput.value = state.domain;
    }
    progressLabel.textContent = `${state.completed}/${state.total}`;

    urlList.innerHTML = "";
    state.items.forEach((item) => {
      urlList.appendChild(renderItem(item));
    });

    generateBtn.disabled = state.isRunning || state.items.length === 0;
  }

  function updateItem(item) {
    const selector = `[data-url="${CSS.escape(item.url)}"]`;
    const li = urlList.querySelector(selector);
    if (!li) {
      return;
    }

    li.className = ["url-item", statusClass(item.status)].filter(Boolean).join(" ");

    const checkbox = li.querySelector("input[type='checkbox']");
    if (checkbox) {
      checkbox.checked = item.selected;
    }

    const chip = li.querySelector(".status-chip");
    if (chip) {
      chip.textContent = item.statusDetail ? `(${item.statusDetail})` : "";
    }
  }

  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/sitemapHub")
    .withAutomaticReconnect()
    .build();

  connection.on("StateUpdated", renderState);
  connection.on("ItemUpdated", updateItem);

  connection
    .start()
    .then(() => connection.invoke("GetState"))
    .catch((err) => console.error(err));

  loadBtn.addEventListener("click", () => {
    const value = domainInput.value.trim();
    if (!value) {
      return;
    }
    connection.invoke("LoadDomain", value).catch((err) => console.error(err));
  });

  generateBtn.addEventListener("click", () => {
    const selected = Array.from(urlList.querySelectorAll("input[type='checkbox']"))
      .filter((input) => input.checked)
      .map((input) => input.dataset.url);

    connection.invoke("StartGenerate", selected).catch((err) => console.error(err));
  });
})();
