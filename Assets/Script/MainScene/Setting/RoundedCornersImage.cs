using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Image))]
public class RoundedCornersImage : BaseMeshEffect
{
    [SerializeField]
    private float _length = 0f;
    [SerializeField, Range(3, 50)]
    private int _division = 10;

    private List<UIVertex> _vertexList = new List<UIVertex>();
    private Vector2 _min;
    private Vector2 _max;
    private float _offset;
    private Color _color;

    public override void ModifyMesh(VertexHelper helper)
    {
        _color = (graphic as Image).color;
        _vertexList.Clear();
        helper.GetUIVertexStream(_vertexList);

        _min = new Vector2(_vertexList[0].position.x, _vertexList[0].position.y);
        _max = new Vector2(_vertexList[0].position.x, _vertexList[0].position.y);
        foreach (var vertex in _vertexList)
        {
            for (var i = 0; i < 2; i++)
            {
                if (_min[i] > vertex.position[i])
                    _min[i] = vertex.position[i];
                if (_max[i] < vertex.position[i])
                    _max[i] = vertex.position[i];
            }
        }

        _offset = Mathf.Clamp(_length, 0f, Mathf.Min((_max[0] - _min[0]) / 2f, (_max[1] - _min[1]) / 2f));

        // “à‘¤‚ÉŠñ‚¹‚é
        // ¶‰º
        SetPosition(0, _offset, _offset);
        SetPosition(5, _offset, _offset);
        // ¶ã
        SetPosition(1, _offset, -_offset);
        // ‰Eã
        SetPosition(2, -_offset, -_offset);
        SetPosition(3, -_offset, -_offset);
        // ‰E‰º
        SetPosition(4, -_offset, _offset);
        // ã‰º¶‰E‚ÌƒfƒR
        CreateDeco();
        // ŠpŠÛ•”•ª
        CreateCircle();
        SetUV();
        helper.Clear();
        helper.AddUIVertexTriangleStream(_vertexList);
    }
    private void SetUV()
    {
        for (var i = 0; i < _vertexList.Count; i++)
        {
            var vertex = _vertexList[i];
            var uv = vertex.uv0;
            for (var j = 0; j < 2; j++)
                uv[j] = Mathf.InverseLerp(_min[j], _max[j], vertex.position[j]);
            vertex.uv0 = uv;
            _vertexList[i] = vertex;
        }
    }
    private void SetPosition(int index, float _offsetX, float _offsetY)
    {
        var vertex = _vertexList[index];
        var pos = vertex.position;
        pos += new Vector3(_offsetX, _offsetY);
        vertex.position = pos;
        _vertexList[index] = vertex;
    }
    private void CreateDeco()
    {
        // ¶‚ÉL‚Î‚·
        Deco(new[]
        {
            new Vector3(_min[0], _min[1] + _offset),
            new Vector3(_min[0], _max[1] - _offset),
            new Vector3(_min[0] + _offset, _max[1] - _offset),
            new Vector3(_min[0] + _offset, _max[1] - _offset),
            new Vector3(_min[0] + _offset, _min[1] + _offset),
            new Vector3(_min[0], _min[1] + _offset),
        });
        // ‰E‚ÉL‚Î‚·
        Deco(new[]
        {
            new Vector3(_max[0] - _offset, _min[1] + _offset),
            new Vector3(_max[0] - _offset, _max[1] - _offset),
            new Vector3(_max[0], _max[1] - _offset),
            new Vector3(_max[0], _max[1] - _offset),
            new Vector3(_max[0], _min[1] + _offset),
            new Vector3(_max[0] - _offset, _min[1] + _offset),
        });
        // ã‚ÉL‚Î‚·
        Deco(new[]
        {
            new Vector3(_min[0] + _offset, _max[1] - _offset),
            new Vector3(_min[0] + _offset, _max[1]),
            new Vector3(_max[0] - _offset, _max[1]),
            new Vector3(_max[0] - _offset, _max[1]),
            new Vector3(_max[0] - _offset, _max[1] - _offset),
            new Vector3(_min[0] + _offset, _max[1] - _offset),
        });
        // ‰º‚ÉL‚Î‚·
        Deco(new[]
        {
            new Vector3(_min[0] + _offset, _min[1]),
            new Vector3(_min[0] + _offset, _min[1] + _offset),
            new Vector3(_max[0] - _offset, _min[1] + _offset),
            new Vector3(_max[0] - _offset, _min[1] + _offset),
            new Vector3(_max[0] - _offset, _min[1]),
            new Vector3(_min[0] + _offset, _min[1]),
        });
    }
    private void Deco(Vector3[] positions)
    {
        foreach (var position in positions)
        {
            _vertexList.Add(new UIVertex
            {
                position = position,
                color = _color,
            });
        }
    }
    private void CreateCircle()
    {
        // ¶ã
        CreateQuadCirclePoints(
            new Vector2(_min[0] + _offset, _max[1] - _offset),
            90f
        );
        // ‰Eã
        CreateQuadCirclePoints(
            new Vector2(_max[0] - _offset, _max[1] - _offset),
            0f
        );
        // ¶‰º
        CreateQuadCirclePoints(
            new Vector2(_min[0] + _offset, _min[1] + _offset),
            180f
        );
        // ‰Eã
        CreateQuadCirclePoints(
            new Vector2(_max[0] - _offset, _min[1] + _offset),
            270f
        );

    }

    private void CreateQuadCirclePoints(Vector2 centerPos, float startAngle)
    {
        var points = new List<Vector2>();
        var addAngle = Mathf.CeilToInt(90f / _division);
        for (var i = 0; i < 90f; i += addAngle)
            points.Add(centerPos + GetCirclePosition(startAngle + i, _offset));

        points.Add(centerPos + GetCirclePosition(startAngle + 90f, _offset));

        for (var i = 0; i < points.Count - 1; i++)
        {
            _vertexList.Add(new UIVertex
            {
                position = new Vector3(centerPos.x, centerPos.y),
                color = _color,
            });
            _vertexList.Add(new UIVertex
            {
                position = new Vector3(points[i].x, points[i].y),
                color = _color,
            });
            _vertexList.Add(new UIVertex
            {
                position = new Vector3(points[i + 1].x, points[i + 1].y),
                color = _color,
            });
        }
    }

    private static Vector2 GetCirclePosition(float angle, float radius)
    {
        return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
    }
}
