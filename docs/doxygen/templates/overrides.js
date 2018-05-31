// overrides.js

var searchbarDiv;
var searchbarInput;

function initOverrides() {
  searchbarInput = $("#MSearchField");
  searchbarDiv = $("#searchbar");

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

  if (searchbarInput !== undefined) {
    searchbarInput.css({width:navWidth - 32 + "px"});
    searchbarDiv.css({width:navWidth + "px"})
  }
}
