/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Infix;
using UnityEngine;

namespace Leap.Unity.Geometry {

  using UnityRect = UnityEngine.Rect;

  public enum VerticalOrigin { Top, Bottom }

  public struct LeapGrid {

    public LocalRect rect;
    public int numRows;
    public int numCols;
    public Margins cellMargins;
    public VerticalOrigin verticalOrigin;
    public bool rowMajor;

    public LeapGrid(UnityRect rect, int numRows = 1, int numCols = 1,
      Margins? cellMargins = null, VerticalOrigin? verticalOrigin = null,
      bool rowMajor = false) :
      this(new LocalRect(rect), numRows, numCols, cellMargins, verticalOrigin,
        rowMajor) { }

    public LeapGrid(LocalRect rect, int numRows = 1, int numCols = 1,
      Margins? cellMargins = null, VerticalOrigin? verticalOrigin = null,
      bool rowMajor = false)
    {
      var useMargins = cellMargins.UnwrapOr(Margins.All(0f));
      var useVertOrigin = verticalOrigin.UnwrapOr(VerticalOrigin.Bottom);

      this.rect = rect;
      this.numRows = numRows;
      this.numCols = numCols;
      this.cellMargins = useMargins;
      this.verticalOrigin = useVertOrigin;
      this.rowMajor = rowMajor;
    }

    public int numCells { get { return numRows * numCols; } }

    public Cell this[int idx] {
      get {
        var cellWidth = rect.radii.x * 2f / numCols;
        var cellHeight = rect.radii.y * 2f / numRows;
        int row = idx / numCols;
        int col = idx % numCols;
        if (rowMajor) {
          row = idx % numRows;
          col = idx / numRows;
          idx = row * numCols + col;
        }
        var origin = verticalOrigin == VerticalOrigin.Bottom ?
          rect.corner00 : rect.corner10;
        var centerFromOrigin = new Vector3(
          col * cellWidth + (cellWidth / 2f),
          row * cellHeight + (cellHeight / 2f),
          0f
        ).CompMul(verticalOrigin == VerticalOrigin.Top ?
          new Vector3(1f, -1f, 1f) : Vector3.one);
        return new Cell() {
          row = row,
          col = col,
          index = idx,
          margins = cellMargins,
          outerRect = new LocalRect() {
            center = origin + centerFromOrigin,
            radii = new Vector2(cellWidth / 2f, cellHeight / 2f)
          }
        };
      }
    }

    public Cell GetMerged(int idx0, int idx1) {
      if (idx0 == idx1) { return this[idx0]; }
      
      int row0 = getRow(idx0), row1 = getRow(idx1);
      if (row0 > row1) { Utils.Swap(ref row0, ref row1); }
      var rowStart = getRow(idx0);
      var numMergedRows = 1 + (row1 - row0);

      int col0 = getCol(idx0), col1 = getCol(idx1);
      if (col0 > col1) { Utils.Swap(ref col0, ref col1); }
      var colStart = getCol(idx0);
      var numMergedCols = 1 + (col1 - col0);

      var cellWidth = rect.radii.x * 2f / this.numCols;
      var cellHeight = rect.radii.y * 2f / this.numRows;
      var mergedCellWidth = cellWidth * numMergedCols;
      var mergedCellHeight = cellHeight * numMergedRows;

      var origin = verticalOrigin == VerticalOrigin.Bottom ?
          rect.corner00 : rect.corner10;
      var centerFromOrigin = new Vector3(
        colStart * cellWidth + (mergedCellWidth / 2f),
        rowStart * cellHeight + (mergedCellHeight / 2f),
        0f
      ).CompMul(verticalOrigin == VerticalOrigin.Top ?
        new Vector3(1f, -1f, 1f) : Vector3.one);

      return new Cell() {
        row = rowStart,
        col = colStart,
        index = (colStart + this.numCols * rowStart),
        margins = cellMargins,
        outerRect = new LocalRect() {
          center = origin + centerFromOrigin,
          radii = new Vector2(mergedCellWidth / 2f, mergedCellHeight / 2f)
        }
      };
    }

    private int getRow(int idx) { return idx / numCols; }
    private int getCol(int idx) { return idx % numCols; }
    private int getIndex(int row, int col) { return row * numCols + col; }

    public struct Cell {
      public int row;
      public int col;
      public int index;
      public Margins margins;
      public LocalRect outerRect;
      public LocalRect innerRect {
        get { return outerRect.PadInner(margins); }
      }
      /// <summary> Shorthand for cell.innerRect. </summary>
      public LocalRect rect { get { return innerRect; } }
      public UnityRect unityRect { get { return rect.ToUnityRect(); }}
    }

    public CellEnumerator GetEnumerator() {
      return new CellEnumerator(this);
    }

    /// <summary> Returns a CellEnumerator that enumerates cells within
    /// the rectangular subgrid defined by cell indices at two opposite
    /// corners of the subgrid. </summary>
    public CellEnumerator EnumerateCells(int subGridBegin, int subGridEnd) {
      return new CellEnumerator(this, subGridBegin, subGridEnd);
    }

    public struct CellEnumerator {

      public LeapGrid grid;
      public int idx;
      public float cellWidth;
      public float cellHeight;

      bool _useSubGrid;
      int _lastIndex;
      int _col0, _col1;

      public CellEnumerator(LeapGrid grid) {
        this.grid = grid;
        this.idx = -1;
        this.cellWidth = grid.rect.radii.x * 2f / grid.numCols;
        this.cellHeight = grid.rect.radii.y * 2f / grid.numRows;

        _useSubGrid = false;
        _col0 = 0; _col1 = 0;
        _lastIndex = grid.numCells - 1;
      }
      public CellEnumerator GetEnumerator() { return this; }

      /// <summary> Initializes a CellEnumerator that enumerates cells within
      /// the rectangular subgrid defined by cell indices at two opposite
      /// corners of the subgrid. </summary>
      public CellEnumerator(LeapGrid grid, int subGridBegin, int subGridEnd) :
        this(grid)
      {
        int idx0 = subGridBegin, idx1 = subGridEnd;
        int row0 = grid.getRow(idx0), row1 = grid.getRow(idx1);
        if (row0 > row1) { Utils.Swap(ref row0, ref row1); }
        int col0 = grid.getCol(idx0), col1 = grid.getCol(idx1);
        if (col0 > col1) { Utils.Swap(ref col0, ref col1); }
        _col0 = col0; _col1 = col1;

        _lastIndex = Mathf.Max(subGridBegin, subGridEnd);
        idx = grid.getIndex(row0, col0) - 1;
        _useSubGrid = true;
      }

      public bool MoveNext() {
        idx += 1;

        if (_useSubGrid) {
          //var row = grid.getRow(idx);
          var col = grid.getCol(idx);
          if (col < _col0) { idx += _col0 - col; }
          if (col > _col1) { idx += grid.numCols - col; }
          if (idx > _lastIndex) { return false; }
        }

        if (idx >= grid.numCells) { return false; }
        else { return true; }
      }
      public Cell Current {
        get { return grid[idx]; }
      }

    }

  }

}
