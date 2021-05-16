#ifndef UIFUNCTIONS_INCLUDED
#define UIFUNCTIONS_INCLUDED
#ifndef PI
#define PI 3.1415926538
#endif



float SDFBorderSingleCorner(float2 UV,float2 radius,float2 Corner,float2 BorderWidth,out bool IsCorner) {
    float2 outerRadius = radius;
    float2 innerRadius = (outerRadius - BorderWidth);
    bool2 negativeInnerRadius = bool2(innerRadius.x < 0,innerRadius.y < 0);
    outerRadius += negativeInnerRadius*(abs(innerRadius));
    innerRadius += negativeInnerRadius*(abs(innerRadius));
    float2 origin = {
        Corner.x == 0 ? Corner.x + outerRadius.x : Corner.x - outerRadius.x,
        Corner.y == 0 ? Corner.y + outerRadius.y : Corner.y - outerRadius.y
    };
    float angleToOrigin = atan2(UV.y - origin.y,UV.x - origin.x);
    float distanceToOrigin = distance(UV,origin);
    float2 ellipseRadius = {
        (innerRadius.x*innerRadius.y)/sqrt(pow(innerRadius.x,2)*pow(sin(angleToOrigin),2) +pow(innerRadius.y,2)*pow(cos(angleToOrigin),2)),
        (outerRadius.x*outerRadius.y)/sqrt(pow(outerRadius.x,2)*pow(sin(angleToOrigin),2) +pow(outerRadius.y,2)*pow(cos(angleToOrigin),2))
    };
    float2 p1 = {
        origin.x,Corner.y
    };
    float2 p2 = {
        Corner.x,origin.y
    };
    float2 angleBounds = {
        atan2(Corner.y - origin.y,0),
        atan2(0,Corner.x - origin.x),
    };
    
    angleBounds = float2(
        min(angleBounds.x,angleBounds.y),
        max(angleBounds.x,angleBounds.y)
    );
    float angleFlip = (angleBounds.y - angleBounds.x) > PI;
    angleBounds.x *= angleFlip && (angleBounds.x >= PI) ? -1 : 1;
    angleBounds.y *= angleFlip && (angleBounds.y >= PI) ? -1 : 1;
    angleBounds = float2(
        min(angleBounds.x,angleBounds.y),
        max(angleBounds.x,angleBounds.y)
    );
    IsCorner = angleToOrigin >= angleBounds.x && angleToOrigin < angleBounds.y && (length(radius) > 0);
    return angleToOrigin >= angleBounds.x && angleToOrigin < angleBounds.y && distanceToOrigin >= ellipseRadius.x && distanceToOrigin < ellipseRadius.y;
    //return smoothstep(angleBounds.x,angleBounds.y,angleToOrigin) * smoothstep(ellipseRadius.x,ellipseRadius.y,distanceToOrigin);
}
float SDFBorderSingleSide(float UV,float Side,float BorderWidth,out float isSide) {
    float2 span = {
        Side - ((Side == 1)*BorderWidth),
        Side + ((Side == 0)*BorderWidth)
    };
    isSide = UV >= span.x && UV < span.y;
    return isSide;
}
bool IsOverlapping(float4 SDFCorners) {
    bool isSet = false;
    bool isOverlapping;
    bool comparison;
    comparison = SDFCorners.x > 0;
    isOverlapping = isOverlapping || (isSet && comparison);
    isSet = isSet || comparison;

    comparison = SDFCorners.y > 0;
    isOverlapping = isOverlapping || (isSet && comparison);
    isSet = isSet || comparison;

    comparison = SDFCorners.z > 0;
    isOverlapping = isOverlapping || (isSet && comparison);
    isSet = isSet || comparison;

    comparison = SDFCorners.w > 0;
    isOverlapping = isOverlapping || (isSet && comparison);
    isSet = isSet || comparison;
    return isOverlapping;
}
void SDFBorder_float(float2 UV,float4 BorderWidth,float4 BorderHorizontalRadius,float4 BorderVerticalRadius,float4x4 BorderColor,float Factor,out float4 SDF) {
    bool4 isCorner;
    bool4 isSide;
    float4 corner = float4(
        SDFBorderSingleCorner(UV,clamp(float2(BorderHorizontalRadius.x*Factor,BorderVerticalRadius.x*Factor),0,1),float2(0, 1),float2(BorderWidth.w,BorderWidth.x),isCorner.x),
        SDFBorderSingleCorner(UV,clamp(float2(BorderHorizontalRadius.y*Factor,BorderVerticalRadius.y*Factor),0,1),float2(1, 1),float2(BorderWidth.y,BorderWidth.x),isCorner.y),
        SDFBorderSingleCorner(UV,clamp(float2(BorderHorizontalRadius.z*Factor,BorderVerticalRadius.z*Factor),0,1),float2(1, 0),float2(BorderWidth.y,BorderWidth.z),isCorner.z),
        SDFBorderSingleCorner(UV,clamp(float2(BorderHorizontalRadius.w*Factor,BorderVerticalRadius.w*Factor),0,1),float2(0, 0),float2(BorderWidth.w,BorderWidth.z),isCorner.w)
    );
    //bool overlapping = IsOverlapping(SDFCorner);
    
    float4 side = float4(
        SDFBorderSingleSide(UV.y,1,BorderWidth.x,isSide.x),
        SDFBorderSingleSide(UV.x,1,BorderWidth.y,isSide.y),
        SDFBorderSingleSide(UV.y,0,BorderWidth.z,isSide.z),
        SDFBorderSingleSide(UV.x,0,BorderWidth.w,isSide.w)
    );
    float4 outerDistances = float4(
        distance(UV.y,1),
        distance(UV.x,1),
        distance(UV.y,0),
        distance(UV.x,0)
    );
    float4 innerDistances = float4(
        max((1 - BorderWidth.x) - UV.y,0),
        max((1 - BorderWidth.y) - UV.x,0),
        max(UV.y - BorderWidth.z,0),
        max(UV.x - BorderWidth.w,0)
    );
    float minInnerDistance = min(min(min(innerDistances.x,innerDistances.y),innerDistances.z),innerDistances.w);
    innerDistances = float4(
        innerDistances.x <= minInnerDistance,
        innerDistances.y <= minInnerDistance,
        innerDistances.z <= minInnerDistance,
        innerDistances.w <= minInnerDistance
    );
    bool useInnerDistance = (innerDistances.x + innerDistances.y + innerDistances.z + innerDistances.w) <= 1;
    float minOuterDistance = min(min(min(outerDistances.x,outerDistances.y),outerDistances.z),outerDistances.w);
    outerDistances = float4(
        outerDistances.x <= minOuterDistance,
        outerDistances.y <= minOuterDistance,
        outerDistances.z <= minOuterDistance,
        outerDistances.w <= minOuterDistance
    );
    float4 border = {
        side.x*(!isCorner.x)*(!isCorner.y) + corner.x + corner.y,
        side.y*(!isCorner.y)*(!isCorner.z) + corner.y + corner.z,
        side.z*(!isCorner.z)*(!isCorner.w) + corner.z + corner.w,
        side.w*(!isCorner.w)*(!isCorner.x) + corner.w + corner.x
    };
    SDF = mul(transpose(BorderColor),border*((!useInnerDistance* outerDistances)+(useInnerDistance* innerDistances)));
    
}


/* void SDFBorderCornerSingleRadius(float2 UV,float Radius,float2 Corner,float2 BorderWidth) {
    float2 p1 = { 
        Corner.x == 0 ? Corner.x + Radius.x : Corner.x - Radius.x,
        Corner.y
    };
    float2 p2 = { 
        Corner.x
        Corner.y == 0 ? Corner.y + Radius.y : Corner.y - Radius.y
    };
    float2 d1 = {
        Corner.x - p1.x,
        Corner.y - p1.y,
    }
    float d1Length = length(d1);
    float2 d2 = {
        Corner.x - p2.x,
        Corner.y - p2.y,
    }
    float d2Length = length(d2);
    float angle = atan2(d1.y,d1.x) - atan2(d2.y,d2.x);
    float segmentLength = Radius / abs(tan(angle));

    float minLength = min(d1Length,d2Length);
    if (segmentLength > minLength) {
        segmentLength = minLength;
        Radius = minLength*abs(tan(angle));
    }
    float2 p1Cross = GetProportionalPoint(Corner,segmentLength,d1Length,d1);
    float2 p2Cross = GetProportionalPoint(Corner,segmentLength,d2Length,d2);

    float2 dO = {
        Corner.x*2 - p1Cross.x - p2Cross.x,
        Corner.y*2 - p1Cross.y - p2Cross.y,
    };
    float dOLength = length(dO);
    float2 origin = GetProportionalPoint(Corner,length(float2(segment,Radius)),dOLength,dO);
    float2 angles = {
        atan2(p1Cross.y - origin.Y,p1Cross.x - origin.X),
        atan2(p2Cross.y - origin.Y,p2Cross.x - origin.X),
    };
    float sweepAngle = angles.y - angles.x;

}
float2 GetProportionalPoint(float2 p,float segment,float length,float2 d ) {
    float factor = segment/length;
    return {
        p.X - d.x * factor,
        p.y - d.y * factor,
    };
} */
/* void SDFBorderCorner(float2 UV,float2 Radius,float2 Corner,float2 BorderWidth) {
    float2 P1 = { 
        Corner.x == 0 ? Corner.x + Radius.x : Corner.x - Radius.x,
        Corner.y
    };
    float2 P2 = { 
        Corner.x
        Corner.y == 0 ? Corner.y + Radius.y : Corner.y - Radius.y
    };

    float angle = atan(Corner.y - P1.y,Corner.x - P1.x) - atan(Corner.y - P2.y,Corner.x - P2.x);
    float2 segment = {
        Radius.x / abs(tan(angle/2)),
        Radius.y / abs(tan(angle/2))
    };
    float2 lengths = {
        sqrt(pow(Corner.x - P1.x,2) + pow(Corner.y - P1.y)),
        sqrt(pow(Corner.x - P2.x,2) + pow(Corner.y - P2.y))
    };
    if (segment.x > lengths.x) {
        Radius.x = length.x * abs(tan(angle/2));
    }
    if (segment.y > lengths.y) {
        Radius.y = length.y * abs(tan(angle/2));
    }
    
} */

void Border_float(float2 UV,float4 BorderWidth,out float4 SDF) {
    
    
float4 edgeDistance = float4(distance(UV.y,1)  ,distance(UV.x,1),distance(UV.y,0),distance(UV.x,0));
SDF = float4(
    edgeDistance.x < BorderWidth.x,
    edgeDistance.y < BorderWidth.y,
    edgeDistance.z < BorderWidth.z,
    edgeDistance.w < BorderWidth.w
);
SDF.x *= (!SDF.y || edgeDistance.x <= edgeDistance.y);
SDF.x *= (!SDF.w || edgeDistance.x <= edgeDistance.w);

SDF.y *= (!SDF.z || edgeDistance.y < edgeDistance.z);
SDF.y *= (!SDF.x || edgeDistance.y < edgeDistance.x);

SDF.z *= (!SDF.w || edgeDistance.z <= edgeDistance.w);
SDF.z *= (!SDF.y || edgeDistance.z <= edgeDistance.y);

SDF.w *= (!SDF.x || edgeDistance.w < edgeDistance.x);
SDF.w *= (!SDF.z || edgeDistance.w < edgeDistance.z);



return;
}

void BorderRadius_float(float2 UV,float4 BorderRadiusHorizontal,float4 BorderRadiusVertical,float4 BorderWidth,out float SDF) {
    float4x2 BorderRadiusInner = { 
        BorderRadiusHorizontal.x, BorderRadiusVertical.x,
        BorderRadiusHorizontal.y, BorderRadiusVertical.y,
        BorderRadiusHorizontal.z, BorderRadiusVertical.z,
        BorderRadiusHorizontal.w, BorderRadiusVertical.w
    };
    float4x2 BorderRadiusOuter = { 
        BorderRadiusHorizontal.x+BorderWidth.w, BorderRadiusVertical.x+BorderWidth.x,
        BorderRadiusHorizontal.y+BorderWidth.y, BorderRadiusVertical.y+BorderWidth.x,
        BorderRadiusHorizontal.z+BorderWidth.y, BorderRadiusVertical.z+BorderWidth.z,
        BorderRadiusHorizontal.w+BorderWidth.w, BorderRadiusVertical.w+BorderWidth.z
    };
    float4x2 p = {
BorderRadiusOuter[0].x,1-BorderRadiusOuter[0].y,
1-BorderRadiusOuter[1].x,1-BorderRadiusOuter[1].y,
1-BorderRadiusOuter[2].x,BorderRadiusOuter[2].y,
BorderRadiusOuter[3].x,BorderRadiusOuter[3].y
    };
    float4 d = {
        distance(UV,p[0]),
        distance(UV,p[1]),
        distance(UV,p[2]),
        distance(UV,p[3])
    };
    float4 d3  = float4(distance(UV.y,1)  ,distance(UV.x,1),distance(UV.y,0),distance(UV.x,0));

        float4 a = {
atan2(UV.y - p[0].y,UV.x - p[0].x),
atan2(UV.y - p[1].y,UV.x - p[1].x),
atan2(UV.y - p[2].y,UV.x - p[2].x),
atan2(UV.y - p[3].y,UV.x - p[3].x)
        };
        float4 rO = {
(BorderRadiusOuter[0].x*BorderRadiusOuter[0].y)/sqrt(pow(BorderRadiusOuter[0].x,2)*pow(sin(a.x),2)+pow(BorderRadiusOuter[0].y,2)*pow(cos(a.x),2)),
(BorderRadiusOuter[1].x*BorderRadiusOuter[1].y)/sqrt(pow(BorderRadiusOuter[1].x,2)*pow(sin(a.y),2)+pow(BorderRadiusOuter[1].y,2)*pow(cos(a.y),2)),
(BorderRadiusOuter[2].x*BorderRadiusOuter[2].y)/sqrt(pow(BorderRadiusOuter[2].x,2)*pow(sin(a.z),2)+pow(BorderRadiusOuter[2].y,2)*pow(cos(a.z),2)),
(BorderRadiusOuter[3].x*BorderRadiusOuter[3].y)/sqrt(pow(BorderRadiusOuter[3].x,2)*pow(sin(a.w),2)+pow(BorderRadiusOuter[3].y,2)*pow(cos(a.w),2))
        };
                float4 rI = {
(BorderRadiusInner[0].x*BorderRadiusInner[0].y)/sqrt(pow(BorderRadiusInner[0].x,2)*pow(sin(a.x),2)+pow(BorderRadiusInner[0].y,2)*pow(cos(a.x),2)),
(BorderRadiusInner[1].x*BorderRadiusInner[1].y)/sqrt(pow(BorderRadiusInner[1].x,2)*pow(sin(a.y),2)+pow(BorderRadiusInner[1].y,2)*pow(cos(a.y),2)),
(BorderRadiusInner[2].x*BorderRadiusInner[2].y)/sqrt(pow(BorderRadiusInner[2].x,2)*pow(sin(a.z),2)+pow(BorderRadiusInner[2].y,2)*pow(cos(a.z),2)),
(BorderRadiusInner[3].x*BorderRadiusInner[3].y)/sqrt(pow(BorderRadiusInner[3].x,2)*pow(sin(a.w),2)+pow(BorderRadiusInner[3].y,2)*pow(cos(a.w),2))
        };
        float4 corners = {
            (UV.x < p[0].x && UV.y >= p[0].y),
            (UV.x >= p[1].x && UV.y >= p[1].y),
            (UV.x >= p[2].x && UV.y < p[2].y),
            (UV.x < p[3].x && UV.y < p[3].y)
        };
        SDF = ((d.x <= rO.x && d.x >= rI.x && corners.x)  ||
                (d.y <= rO.y && d.y >= rI.y && corners.y) ||
                (d.z <= rO.z && d.z >= rI.z && corners.z) ||
                (d.w <= rO.w && d.w >= rI.w && corners.w)) || 
                (!(any(corners)) && 
                    (d3.x <= BorderWidth.x || 
                    d3.y <= BorderWidth.y || 
                    d3.z <= BorderWidth.z || 
                    d3.w <= BorderWidth.w));
            

return;
}
#endif