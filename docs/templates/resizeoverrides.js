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
}
$(document).ready(readyInit);