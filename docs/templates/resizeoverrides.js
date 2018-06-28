// resize_overrides.js

// var searchbarDiv;
// var searchbarInput;

function initOverrides() {
  // searchbarInput = $("#MSearchField");
  // searchbarDiv = $("#searchbar");

  var width = readCookie('width');
  if (width) { restoreWidth(width); } else { resizeWidth(); }
}

$(document).ready(initOverrides);

function resizeHeight() {
  var headerHeight = header.outerHeight();
  var footerHeight = footer.outerHeight();
  var windowHeight = $(window).height() - headerHeight;
  navtree.css({height:windowHeight + "px"});
  sidenav.css({height:windowHeight + "px"});

  var contentHeight = $(window).height();
  content.css({height:contentHeight + "px"});

  var sidenavTop = headerHeight;
  sidenav.css({top:sidenavTop + "px"})
}

$(window).load(resizeHeight);

function restoreWidth(navWidth)
{
  var windowWidth = $(window).width() + "px";
  content.css({marginLeft:parseInt(navWidth)+"px"});
  sidenav.css({width:navWidth + "px"});

  // if (searchbarInput !== undefined) {
  //   searchbarInput.css({width:navWidth - 32 + "px"});
  //   searchbarDiv.css({width:navWidth + "px"})
  // }
}

function readyInit() {
  var docContent = document.getElementById(`doc-content`);
  var contentContainer = document.createElement('div');
  contentContainer.id = 'doc-content-container'

  docContent.parentNode.insertBefore(contentContainer, docContent);
  $(docContent).detach().appendTo(contentContainer);

  content = $('#doc-content-container')


  // mutation observer
  let mSearchResults = document.getElementById("MSearchResultsWindow");

  // Options for the observer (which mutations to observe)
  let config = { attributes: true, childList: true, subtree: true,
                 characterData: true };

  // Callback function to execute when mutations are observed
  let callback = function (mutationsList) {
    setTimeout(fixSearchCSS, 100);
  };

  // Create an observer instance linked to the callback function
  var observer = new MutationObserver(callback);

  // Start observing the target node for configured mutations
  observer.observe(mSearchResults, config);
}
$(document).ready(readyInit);

function fixSearchCSS() {
  let cssLink = "../extra.css";

  let searchDocument = document.getElementById("MSearchResults")
                      .contentWindow.document;

  var head = searchDocument.getElementsByTagName("head")[0];
  var customStyle = searchDocument.createElement("link");
  customStyle.setAttribute("href", cssLink);
  customStyle.setAttribute("type", "text/css");
  customStyle.setAttribute("rel", "stylesheet");
  head.appendChild(customStyle);
}
