package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist;

import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.drawable.Drawable;

import com.pixplicity.sharp.Sharp;

import java.io.IOException;
import java.net.URISyntaxException;
import java.net.URLDecoder;

import io.adaptivecards.objectmodel.AdaptiveBase64Util;
import io.adaptivecards.objectmodel.CharVector;
import io.adaptivecards.renderer.GenericImageLoaderAsync;
import io.adaptivecards.renderer.IResourceResolver;
import io.adaptivecards.renderer.Util;
import io.adaptivecards.renderer.http.HttpRequestResult;

public class SvgImageLoader implements IResourceResolver
{
    @Override
    public HttpRequestResult<Bitmap> resolveImageResource(String uri, GenericImageLoaderAsync genericImageLoaderAsync) throws IOException, URISyntaxException
    {
        Bitmap bitmap;
        String dataUri = AdaptiveBase64Util.ExtractDataFromUri(uri);
        CharVector decodedDataUri = AdaptiveBase64Util.Decode(dataUri);
        byte[] decodedByteArray = Util.getBytes(decodedDataUri);
        bitmap = BitmapFactory.decodeByteArray(decodedByteArray, 0, decodedByteArray.length);

        return new HttpRequestResult<>(bitmap);
    }

    @Override
    public HttpRequestResult<Bitmap> resolveImageResource(String uri, GenericImageLoaderAsync genericImageLoaderAsync, int maxWidth) throws IOException, URISyntaxException
    {
        Bitmap bitmap;
        if (uri.startsWith("data:image/svg")) {
            String svgString = AdaptiveBase64Util.ExtractDataFromUri(uri);
            CharVector chars = AdaptiveBase64Util.Decode(svgString);
            StringBuilder sb = new StringBuilder();
            for (Character ch : chars){
                sb.append(ch);
            }
            String decodedSvgString  = sb.toString();
            Sharp sharp = Sharp.loadString(decodedSvgString);
            Drawable drawable = sharp.getDrawable();
            bitmap = drawableToBitmap(drawable, maxWidth);
        }
        else
        {
            try
            {
                return genericImageLoaderAsync.loadDataUriImage(uri);
            }
            catch (Exception e)
            {
                return new HttpRequestResult<>(e);
            }
        }

        return new HttpRequestResult<>(bitmap);
    }

    private static Bitmap drawableToBitmap(Drawable drawable, int maxWidth)
    {
        if (drawable instanceof BitmapDrawable){
            return ((BitmapDrawable)drawable).getBitmap();
        }
        else {
            int height = (int)((float)maxWidth * ((float)drawable.getIntrinsicHeight() / (float)drawable.getIntrinsicWidth()));

            Bitmap bitmap;
            try {
                // this can cause OutOfMemoryError
                bitmap = Bitmap.createBitmap(maxWidth, height, Bitmap.Config.ARGB_8888);
            } catch (OutOfMemoryError exception){
                bitmap = Bitmap.createBitmap(0, 0, Bitmap.Config.ARGB_8888);
            }
            Canvas canvas = new Canvas(bitmap);
            drawable.setBounds(0, 0, canvas.getWidth(), canvas.getHeight());
            drawable.draw(canvas);
            return bitmap;
        }
    }
}
